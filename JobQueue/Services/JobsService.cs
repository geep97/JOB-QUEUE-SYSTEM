using JobQueue.Data;
using JobQueue.DTOs;
using JobQueue.Models;
using Microsoft.EntityFrameworkCore;

namespace JobQueue.Services
{
    public class JobsService : IJobsService
    {
        private readonly JobQueueDbContext _context;

        private readonly RabbitMqPublisher _rabbitMqPublisher;

        public JobsService(
        JobQueueDbContext context,
        RabbitMqPublisher rabbitMqPublisher)
        {
            _context = context;
            _rabbitMqPublisher = rabbitMqPublisher;
        }

        public async Task<IEnumerable<Job>> GetAllJobsAsync()
        {
            return await _context.Jobs.AsNoTracking().ToListAsync();
        }

        public async Task<Job?> GetJobByIdAsync(int id)
        {
            return await _context.Jobs.FindAsync(id);
        }

        public async Task<Job> CreateJobAsync(CreateJobDto jobCreateDto)
        {
            var job = new Job
            {
                Type = jobCreateDto.Type,
                Payload = jobCreateDto.Payload,
                Status = JobStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ProcessAfter = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            await _rabbitMqPublisher.PublishJobCreatedAsync(job.Id);
            return job;
        }

        public async Task<Job?> GetNextJobForProcessingAsync(int maxRetries, TimeSpan lockTimeout)
        {
            var cutoffTime = DateTime.UtcNow - lockTimeout;

            return await _context.Jobs
                .AsNoTracking()
                .Where(j => (j.Status == JobStatus.Pending && DateTime.UtcNow >= j.ProcessAfter && j.Attempts < maxRetries)
                         || (j.Status == JobStatus.InProgress && j.ClaimedAt < cutoffTime && j.Attempts < maxRetries))
                .OrderBy(j => j.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Job?> GetJobForProcessingByIdAsync(
    int id,
    int maxRetries,
    TimeSpan lockTimeout)
        {
            var cutoffTime = DateTime.UtcNow - lockTimeout;

            return await _context.Jobs
                .AsNoTracking()
                .Where(j => j.Id == id &&
                    (
                        (j.Status == JobStatus.Pending &&
                         j.ProcessAfter <= DateTime.UtcNow &&
                         j.Attempts < maxRetries)
                        ||
                        (j.Status == JobStatus.InProgress &&
                         j.ClaimedAt < cutoffTime &&
                         j.Attempts < maxRetries)
                    ))
                .FirstOrDefaultAsync();
        }

        public async Task<bool> MarkJobAsProcessingAsync(Job job)
        {
            try
            {
                _context.Entry(job).State = EntityState.Unchanged;

                job.Status = JobStatus.InProgress;
                job.ClaimedAt = DateTime.UtcNow;
                job.Attempts += 1;

                _context.Jobs.Update(job);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
        }

        public async Task<bool> UpdateJobStatusWithBackoffAsync(int id, JobStatus newStatus, string? errorMessage, int currentAttempts)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return false;
            }

            job.Status = newStatus;
            job.ClaimedAt = null;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                job.Errors = errorMessage;
            }

            if (newStatus == JobStatus.Pending)
            {
                int delaySeconds = currentAttempts switch
                {
                    1 => 10,
                    2 => 60,
                    _ => 300
                };
                job.ProcessAfter = DateTime.UtcNow.AddSeconds(delaySeconds);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CleanUpAbandonedJobsAsync(int maxRetries, TimeSpan lockTimeout)
        {
            var cutoffTime = DateTime.UtcNow - lockTimeout;

            var abandonedJobs = await _context.Jobs
                .Where(j => j.Status == JobStatus.InProgress
                         && j.ClaimedAt < cutoffTime
                         && j.Attempts >= maxRetries)
                .ToListAsync();

            foreach (var job in abandonedJobs)
            {
                try
                {
                    job.Status = JobStatus.Failed;
                    job.ClaimedAt = null;
                    job.Errors = "Job abandoned mid-flight with no remaining retries available.";

                    _context.Jobs.Update(job);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    _context.Entry(job).State = EntityState.Detached;
                }
            }
        }

        public async Task<bool> ReleaseJobOnShutdownAsync(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return false;
            }

            try
            {

                job.Status = JobStatus.Pending;
                job.ClaimedAt = null;
                job.Errors = null;
                job.ProcessAfter = DateTime.UtcNow;

                if (job.Attempts > 0)
                {
                    job.Attempts -= 1;
                }

                await _context.SaveChangesAsync();
                return true;

            }

            catch (DbUpdateConcurrencyException)
            {
                _context.Entry(job).State = EntityState.Detached;
                return false;
            }
        }
    }
}

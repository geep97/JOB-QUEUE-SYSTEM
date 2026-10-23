using JobQueue.Data;
using JobQueue.DTOs;
using JobQueue.Models;
using Microsoft.EntityFrameworkCore;

namespace JobQueue.Services
{
    public class JobsService : IJobsService
    {
        private readonly JobQueueDbContext _context;

        public JobsService(JobQueueDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Job>> GetAllJobsAsync()
        {
            return await _context.Jobs.ToListAsync();
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
                CreatedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<Job?> GetNextPendingJobAsync(int maxRetries)
        {
            return await _context.Jobs
              .Where(j => j.Status == JobStatus.Pending && j.Attempts < maxRetries)
              .OrderBy(j => j.CreatedAt)
              .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateJobStatusAsync(int id, JobStatus newStatus, string? errorMessage)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return false;
            }

            job.Status = newStatus;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                job.Errors = errorMessage;
            }

            if (newStatus == JobStatus.InProgress)
            {
                job.Attempts += 1;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

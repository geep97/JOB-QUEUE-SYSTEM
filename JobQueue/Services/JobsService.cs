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
        public async Task<Jobs> CreateJobAsync(CreateJobDto jobCreateDto)
        {
            var job = new Jobs
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
        public async Task<IEnumerable<Jobs>> GetAllJobsAsync()
        {
            return await _context.Jobs.ToListAsync();
        }
        public async Task<Jobs?> GetJobByIdAsync(int id)
        {
            return await _context.Jobs.FindAsync(id);
        }


        public async Task<bool> UpdateJobStatusAsync(int id, JobStatus newStatus, string? errorMessage = null)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return false;
            }
            job.Status = newStatus;


            if (!String.IsNullOrEmpty(errorMessage))
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

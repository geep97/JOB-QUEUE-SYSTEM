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
    }
}

using JobQueue.DTOs;
using JobQueue.Models;

namespace JobQueue.Services
{
    public interface IJobsService
    {
        Task<IEnumerable<Job>> GetAllJobsAsync();
        Task<Job?> GetJobByIdAsync(int id);
        Task<Job> CreateJobAsync(CreateJobDto jobDto);
        Task<Job?> GetNextPendingJobAsync(int maxRetries);
        Task<bool> UpdateJobStatusAsync(int id, JobStatus newStatus, string? errorMessage);
    }
}

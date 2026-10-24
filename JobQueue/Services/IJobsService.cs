using JobQueue.DTOs;
using JobQueue.Models;

namespace JobQueue.Services
{
    public interface IJobsService
    {
        Task<IEnumerable<Job>> GetAllJobsAsync();
        Task<Job?> GetJobByIdAsync(int id);
        Task<Job> CreateJobAsync(CreateJobDto jobDto);
        Task<Job?> GetNextJobForProcessingAsync(int maxRetries, TimeSpan lockTimeout);
        Task<bool> MarkJobAsProcessingAsync(Job job);
        Task<bool> UpdateJobStatusWithBackoffAsync(int id, JobStatus newStatus, string? errorMessage, int currentAttempts);

        Task CleanUpAbandonedJobsAsync(int maxRetries, TimeSpan lockTimeout);


        Task <bool> ReleaseJobOnShutdownAsync(int id);
    }
}

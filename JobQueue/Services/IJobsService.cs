using JobQueue.DTOs;
using JobQueue.Models;



namespace JobQueue.Services
{
    public interface IJobsService
    {
        Task<Jobs> CreateJobAsync(CreateJobDto jobCreateDto);
        Task<IEnumerable<Jobs>> GetAllJobsAsync();
        Task<Jobs?> GetJobByIdAsync(int id);

    }
}

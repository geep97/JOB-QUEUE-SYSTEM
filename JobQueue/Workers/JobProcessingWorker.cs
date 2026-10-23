using JobQueue.Models;
using JobQueue.Services;

namespace JobQueue.Workers
{
    public class JobProcessingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobProcessingWorker> _logger;
        private const int MaxRetries = 3;

        public JobProcessingWorker(IServiceProvider serviceProvider, ILogger<JobProcessingWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job Processing Engine started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var jobsService = scope.ServiceProvider.GetRequiredService<IJobsService>();

                        var allJobs = await jobsService.GetAllJobsAsync();
                        var pendingJob = allJobs
                            .Where(j => j.Status == JobStatus.Pending && j.Attempts < MaxRetries)
                            .OrderBy(j => j.CreatedAt)
                            .FirstOrDefault();

                        if (pendingJob != null)
                        {
                            _logger.LogInformation("Processing Job ID {Id} ({Type}).", pendingJob.Id, pendingJob.Type);

                            await jobsService.UpdateJobStatusAsync(pendingJob.Id, JobStatus.InProgress, null);

                            try
                            {
                                await SimulatedJobExecutor(pendingJob);

                                await jobsService.UpdateJobStatusAsync(pendingJob.Id, JobStatus.Completed, null);
                                _logger.LogInformation("Completed Job ID {Id}.", pendingJob.Id);
                            }
                            catch (Exception taskEx)
                            {
                                _logger.LogWarning("Job ID {Id} failed: {Message}", pendingJob.Id, taskEx.Message);

                                if (pendingJob.Attempts >= MaxRetries)
                                {
                                    await jobsService.UpdateJobStatusAsync(pendingJob.Id, JobStatus.Failed, taskEx.Message);
                                    _logger.LogError("Job ID {Id} moved to Dead-Letter status.", pendingJob.Id);
                                }
                                else
                                {
                                    await jobsService.UpdateJobStatusAsync(pendingJob.Id, JobStatus.Pending, taskEx.Message);
                                    _logger.LogInformation("Job ID {Id} reset to Pending.", pendingJob.Id);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background processing cycle.");
                }

                await Task.Delay(5000, stoppingToken);
            }

            _logger.LogInformation("Job Processing Engine stopped.");
        }

        private async Task SimulatedJobExecutor(Jobs job)
        {
            await Task.Delay(3000);

            if (job.Type.Contains("Fail", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Simulated runtime processing failure.");
            }
        }
    }
}

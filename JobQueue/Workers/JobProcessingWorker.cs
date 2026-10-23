using JobQueue.Models;
using JobQueue.Services;

namespace JobQueue.Workers
{
    public class JobProcessingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobProcessingWorker> _logger;
        private readonly int _maxRetries;

        public JobProcessingWorker(IServiceProvider serviceProvider, ILogger<JobProcessingWorker> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _maxRetries = configuration.GetValue<int>("JobQueueSettings:MaxRetries");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job Processing Engine started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                bool jobProcessed = false;

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var jobsService = scope.ServiceProvider.GetRequiredService<IJobsService>();

                        var pendingJob = await jobsService.GetNextPendingJobAsync(_maxRetries);

                        if (pendingJob != null)
                        {
                            jobProcessed = true;
                            _logger.LogInformation("Processing Job ID {Id} ({Type}).", pendingJob.Id, pendingJob.Type);

                            await jobsService.UpdateJobStatusAsync(pendingJob.Id, JobStatus.Processing, null);

                            try
                            {
                                await SimulatedJobExecutor(pendingJob);
                                await jobsService.UpdateJobStatusAsync(pendingJob.Id, JobStatus.Completed, null);
                                _logger.LogInformation("Completed Job ID {Id}.", pendingJob.Id);
                            }
                            catch (Exception taskEx)
                            {
                                _logger.LogWarning("Job ID {Id} failed: {Message}", pendingJob.Id, taskEx.Message);

                                if (pendingJob.Attempts >= _maxRetries)
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

                if (!jobProcessed)
                {
                    await Task.Delay(5000, stoppingToken);
                }
            }

            _logger.LogInformation("Job Processing Engine stopped.");
        }

        private async Task SimulatedJobExecutor(Job job)
        {
            await Task.Delay(3000);

            if (job.Type.Contains("Fail", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Simulated runtime processing failure.");
            }
        }
    }
}

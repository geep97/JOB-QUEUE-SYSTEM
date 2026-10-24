using JobQueue.Models;
using JobQueue.Services;

namespace JobQueue.Workers
{
    public class JobProcessingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobProcessingWorker> _logger;
        private readonly int _maxRetries;
        private readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(10);
        private readonly RabbitMqConsumer _rabbitMqConsumer;
        private readonly JobNotificationChannel _notificationChannel;

        public JobProcessingWorker(IServiceProvider serviceProvider, ILogger<JobProcessingWorker> logger, IConfiguration configuration, RabbitMqConsumer rabbitMqConsumer,JobNotificationChannel notificationChannel)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _maxRetries = configuration.GetValue<int>("JobQueueSettings:MaxRetries");
            _rabbitMqConsumer = rabbitMqConsumer;
            _notificationChannel = notificationChannel;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job Processing Engine started.");

            var consumerTask = _rabbitMqConsumer.StartAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                bool jobProcessed = false;

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var jobsService = scope.ServiceProvider.GetRequiredService<IJobsService>();

                        await jobsService.CleanUpAbandonedJobsAsync(_maxRetries, _lockTimeout);

                        Job? pendingJob = null;

                        
                        if (_notificationChannel.TryRead(out int notifiedJobId))
                        {
                            pendingJob = await jobsService.GetJobForProcessingByIdAsync(
                                notifiedJobId,
                                _maxRetries,
                                _lockTimeout);

                            _logger.LogInformation(
                           "Received job notification from RabbitMQ for Job ID {Id}.",
                           notifiedJobId);
                        }

                       
                        pendingJob ??= await jobsService.GetNextJobForProcessingAsync(
                            _maxRetries,
                            _lockTimeout);

                        if (pendingJob != null)
                        {
                            bool claimed = await jobsService.MarkJobAsProcessingAsync(pendingJob);

                            if (!claimed)
                            {
                                jobProcessed = true;
                                continue;
                            }

                            jobProcessed = true;
                            _logger.LogInformation("Processing Job ID {Id} ({Type}).", pendingJob.Id, pendingJob.Type);

                            try
                            {
                                await SimulatedJobExecutor(pendingJob, stoppingToken);
                                await jobsService.UpdateJobStatusWithBackoffAsync(pendingJob.Id, JobStatus.Completed, null, pendingJob.Attempts);
                                _logger.LogInformation("Completed Job ID {Id}.", pendingJob.Id);
                            }
                            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                            {
                                _logger.LogWarning(
                                    "Application shutdown initiated during execution. Releasing Job ID {Id} back to the queue.",
                                    pendingJob.Id);

                                bool released = await jobsService.ReleaseJobOnShutdownAsync(pendingJob.Id);

                                if (released)
                                {
                                    _logger.LogInformation(
                                        "Job ID {Id} successfully released back to Pending.",
                                        pendingJob.Id);
                                }
                                else
                                {
                                    _logger.LogWarning(
                                        "Could not release Job ID {Id}. It may have been deleted or changed concurrently.",
                                        pendingJob.Id);
                                }

                                break;
                            }
                            catch (Exception taskEx)
                            {
                                _logger.LogWarning("Job ID {Id} failed: {Message}", pendingJob.Id, taskEx.Message);

                                if (pendingJob.Attempts >= _maxRetries)
                                {
                                    await jobsService.UpdateJobStatusWithBackoffAsync(pendingJob.Id, JobStatus.Failed, taskEx.Message, pendingJob.Attempts);
                                    _logger.LogError("Job ID {Id} moved to Dead-Letter status.", pendingJob.Id);
                                }
                                else
                                {
                                    await jobsService.UpdateJobStatusWithBackoffAsync(pendingJob.Id, JobStatus.Pending, taskEx.Message, pendingJob.Attempts);
                                    _logger.LogInformation("Job ID {Id} reset to Pending with backoff delay.", pendingJob.Id);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background processing cycle.");
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (!jobProcessed)
                {
                    using var waitCts =
                        CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                    waitCts.CancelAfter(TimeSpan.FromSeconds(5));

                    try
                    {
                        await _notificationChannel.WaitToReadAsync(waitCts.Token);
                    }
                    catch (OperationCanceledException)
                        when (!stoppingToken.IsCancellationRequested)
                    {
                        
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("Job Processing Engine stopped.");
        }

        private async Task SimulatedJobExecutor(Job job, CancellationToken cancellationToken)
        {
            for (int i = 0; i < 3; i++) 
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken);
            }

            if (job.Type.Contains("Fail", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Simulated runtime processing failure.");
            }
        }
    }
}

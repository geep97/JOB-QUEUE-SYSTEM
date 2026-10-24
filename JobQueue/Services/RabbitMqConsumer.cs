
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace JobQueue.Services
{
    public class RabbitMqConsumer
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly JobNotificationChannel _notificationChannel;

        public RabbitMqConsumer(
            IConfiguration configuration,
            ILogger<RabbitMqConsumer> logger,
            JobNotificationChannel notificationchannel)
        {
            _configuration = configuration;
            _logger = logger;
            _notificationChannel = notificationchannel;
        }

        public async Task StartAsync(
            CancellationToken cancellationToken)
        {
            var rabbitMqUrl = _configuration["RabbitMq:Url"];

            if (string.IsNullOrWhiteSpace(rabbitMqUrl))
            {
                throw new InvalidOperationException(
                    "RabbitMQ URL is not configured.");
            }

            var factory = new ConnectionFactory
            {
                Uri = new Uri(rabbitMqUrl)
            };

            await using var connection =
                await factory.CreateConnectionAsync(cancellationToken);

            await using var channel =
                await connection.CreateChannelAsync(
                    cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: "job-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false,
                cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, eventArgs) =>
            {
                try
                {
                    var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                    var jobMessage = JsonSerializer.Deserialize<JobMessage>(message);

                    if (jobMessage is null || jobMessage.JobId <= 0)
                    {
                        _logger.LogWarning(
                            "Received an invalid job message: {Message}",
                            message);

                        await channel.BasicNackAsync(
                            eventArgs.DeliveryTag,
                            multiple: false,
                            requeue: false,
                            cancellationToken: cancellationToken);

                        return;
                    }

                    await _notificationChannel.PublishAsync(
                        jobMessage.JobId,
                        cancellationToken);

                    await channel.BasicAckAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to handle a RabbitMQ job message.");

                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: true,
                        cancellationToken: cancellationToken);
                }
            };

            await channel.BasicConsumeAsync(
                queue: "job-queue",
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);

            _logger.LogInformation("RabbitMQ consumer started.");

            try
            {
                await Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                
            }
        }

        private sealed class JobMessage
        {
            public int JobId { get; set; }
        }
    }
}

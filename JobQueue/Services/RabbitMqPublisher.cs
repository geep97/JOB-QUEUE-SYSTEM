
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace JobQueue.Services
{
    public class RabbitMqPublisher : IAsyncDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _publishLock = new(1, 1);

        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqPublisher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task PublishJobCreatedAsync(int jobId)
        {
            await _publishLock.WaitAsync();

            try
            {
                await EnsureConnectedAsync();

                var message = JsonSerializer.Serialize(
                    new { JobId = jobId });

                var body = Encoding.UTF8.GetBytes(message);

                await _channel!.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: "job-queue",
                    body: body);
            }
            catch
            {
                await DisposeConnectionAsync();
                throw;
            }
            finally
            {
                _publishLock.Release();
            }
        }

        private async Task EnsureConnectedAsync()
        {
            if (_connection is { IsOpen: true } &&
                _channel is { IsOpen: true })
            {
                return;
            }

            await DisposeConnectionAsync();

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

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: "job-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        private async Task DisposeConnectionAsync()
        {
            if (_channel is not null)
            {
                await _channel.DisposeAsync();
                _channel = null;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _publishLock.WaitAsync();

            try
            {
                await DisposeConnectionAsync();
            }
            finally
            {
                _publishLock.Release();
                _publishLock.Dispose();
            }
        }
    }
}

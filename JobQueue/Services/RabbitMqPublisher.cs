
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace JobQueue.Services
{
    public class RabbitMqPublisher
    {
        private readonly IConfiguration _configuration;

        public RabbitMqPublisher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task PublishJobCreatedAsync(int jobId)
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
                await factory.CreateConnectionAsync();

            await using var channel =
                await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "job-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var message = JsonSerializer.Serialize(new
            {
                JobId = jobId
            });

            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: "job-queue",
                body: body);
        }
    }
}


using System.Threading.Channels;

namespace JobQueue.Services
{
    public class JobNotificationChannel
    {

        private readonly Channel<int> _channel =
            Channel.CreateBounded<int>(
                new BoundedChannelOptions(100)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false
                });


        public bool TryRead(out int jobId)
        {
            return _channel.Reader.TryRead(out jobId);
        }


        public async ValueTask<int> ReadAsync(
        CancellationToken cancellationToken = default)
          {
            return await _channel.Reader.ReadAsync(cancellationToken);
          }


        public ValueTask<bool> WaitToReadAsync(
            CancellationToken cancellationToken = default)
        {
            return _channel.Reader.WaitToReadAsync(cancellationToken);
        }

        public async ValueTask PublishAsync(
            int jobId,
            CancellationToken cancellationToken = default)
        {
            await _channel.Writer.WriteAsync(jobId, cancellationToken);
        }

        public IAsyncEnumerable<int> ReadAllAsync(
            CancellationToken cancellationToken = default)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}

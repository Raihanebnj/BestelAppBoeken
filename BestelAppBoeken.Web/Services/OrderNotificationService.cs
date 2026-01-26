using System.Threading.Channels;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BestelAppBoeken.Web.Services
{
    public class OrderNotificationService
    {
        private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        public ValueTask PublishAsync(string message, CancellationToken ct = default)
            => _channel.Writer.WriteAsync(message, ct);

        public async IAsyncEnumerable<string> SubscribeAsync([EnumeratorCancellation] CancellationToken ct)
        {
            while (await _channel.Reader.WaitToReadAsync(ct))
            {
                while (_channel.Reader.TryRead(out var item))
                {
                    yield return item;
                }
            }
        }
    }
}

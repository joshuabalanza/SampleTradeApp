using System.Threading.Channels;

namespace TradeOps.Core.Services;

public interface ITradeProcessingQueue
{
    ValueTask EnqueueAsync(Guid tradeId);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}

public class ChannelTradeProcessingQueue : ITradeProcessingQueue
{
    // Bounded capacity of 25,000 trades in memory with backpressure
    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(25000)
    {
        FullMode = BoundedChannelFullMode.Wait, // Applies backpressure if full
        SingleReader = false,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(Guid tradeId) => _channel.Writer.WriteAsync(tradeId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

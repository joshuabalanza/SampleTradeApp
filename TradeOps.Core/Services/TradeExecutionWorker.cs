using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeOps.Core.Models;

namespace TradeOps.Core.Services;

public class TradeExecutionWorker : BackgroundService
{
    private readonly ITradeProcessingQueue _queue;
    private readonly ITradeRepository _repository;
    private readonly ILogger<TradeExecutionWorker> _logger;

    public TradeExecutionWorker(
        ITradeProcessingQueue queue,
        ITradeRepository repository,
        ILogger<TradeExecutionWorker> logger)
    {
        _queue = queue;
        _repository = repository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TradeExecutionWorker started.");

        // Asynchronously read from channel as items become available
        await foreach (var tradeId in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                var trade = await _repository.GetByIdAsync(tradeId);
                if (trade == null || trade.Status != TradeStatus.Pending)
                {
                    continue;
                }

                // Simulate order execution engine or broker routing latency (15ms)
                await Task.Delay(15, stoppingToken);

                // Update trade state to Executed
                await _repository.UpdateStatusAsync(tradeId, TradeStatus.Executed);

                _logger.LogInformation("Trade {TradeId} successfully EXECUTED.", tradeId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed executing trade {TradeId}", tradeId);
                await _repository.UpdateStatusAsync(tradeId, TradeStatus.Failed, ex.Message);
            }
        }

        _logger.LogInformation("TradeExecutionWorker stopped.");
    }
}

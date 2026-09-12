using Microsoft.Extensions.Logging;
using TradeOps.Core.Models;

namespace TradeOps.Core.Services;

public interface IReconciliationService
{
    Task<bool> ReconcileTradeAsync(Guid tradeId, BrokerExecutionReport brokerReport);
}

public class ReconciliationService : IReconciliationService
{
    private readonly ITradeRepository _repository;
    private readonly ILogger<ReconciliationService> _logger;

    // 0.5% maximum allowable price slippage tolerance
    private const decimal SlippageTolerancePercent = 0.005m;

    public ReconciliationService(ITradeRepository repository, ILogger<ReconciliationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> ReconcileTradeAsync(Guid tradeId, BrokerExecutionReport brokerReport)
    {
        var trade = await _repository.GetByIdAsync(tradeId);
        if (trade is null)
        {
            _logger.LogError("Reconciliation aborted: Trade {TradeId} not found.", tradeId);
            return false;
        }

        // Rule 1: Check Symbol
        if (!string.Equals(trade.Symbol, brokerReport.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            var reason = $"Symbol mismatch. Expected {trade.Symbol}, broker reported {brokerReport.Symbol}";
            await FlagDiscrepancyAsync(tradeId, reason);
            return false;
        }

        // Rule 2: Check Order Side
        if (trade.Side != brokerReport.Side)
        {
            var reason = $"Side mismatch. Expected {trade.Side}, broker reported {brokerReport.Side}";
            await FlagDiscrepancyAsync(tradeId, reason);
            return false;
        }

        // Rule 3: Check Quantity (Volume)
        if (trade.Quantity != brokerReport.Quantity)
        {
            var reason = $"Quantity mismatch. Expected {trade.Quantity}, broker executed {brokerReport.Quantity}";
            await FlagDiscrepancyAsync(tradeId, reason);
            return false;
        }

        // Rule 4: Check Price Slippage Drift
        var priceDifference = Math.Abs(trade.Price - brokerReport.ExecutedPrice);
        var maxAllowedDiff = trade.Price * SlippageTolerancePercent;

        if (priceDifference > maxAllowedDiff)
        {
            var reason = $"Slippage exceeded 0.5% tolerance. Requested {trade.Price}, Executed {brokerReport.ExecutedPrice} (Diff: {priceDifference})";
            await FlagDiscrepancyAsync(tradeId, reason);
            return false;
        }

        // All 4 checks pass: Mark as Reconciled
        await _repository.UpdateStatusAsync(tradeId, TradeStatus.Reconciled);
        await _repository.AddAuditLogAsync(
            tradeId,
            "RECONCILED",
            $"Successfully reconciled against broker ref {brokerReport.ExternalTradeId} at price {brokerReport.ExecutedPrice}"
        );

        _logger.LogInformation("Trade {TradeId} successfully reconciled against broker execution {BrokerId}.", tradeId, brokerReport.ExternalTradeId);
        return true;
    }

    private async Task FlagDiscrepancyAsync(Guid tradeId, string reason)
    {
        _logger.LogWarning("Trade {TradeId} flagged with discrepancy: {Reason}", tradeId, reason);
        await _repository.UpdateStatusAsync(tradeId, TradeStatus.Discrepancy, reason);
    }
}

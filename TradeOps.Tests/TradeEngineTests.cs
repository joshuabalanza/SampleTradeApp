using Microsoft.Extensions.Logging.Abstractions;
using TradeOps.Core.Models;
using TradeOps.Core.Services;
using Xunit;

namespace TradeOps.Tests;

public class TradeEngineTests
{
    private readonly InMemoryTradeRepository _repository = new();
    private readonly ReconciliationService _reconciliationService;

    public TradeEngineTests()
    {
        _reconciliationService = new ReconciliationService(
            _repository,
            NullLogger<ReconciliationService>.Instance
        );
    }

    [Fact]
    public async Task IngestTrade_WhenDuplicateIdempotencyKeyProvided_ReturnsExistingTradeWithoutDuplicating()
    {
        // 1. Arrange: First trade request
        var request = new IngestTradeRequest(
            IdempotencyKey: "CLIENT-ORDER-KEY-9999",
            AccountId: "ACC-JOSHUA-1",
            Symbol: "EURUSD",
            Side: OrderSide.Buy,
            Quantity: 100000m,
            Price: 1.0850m
        );

        // 2. Act: Send first time
        var (firstTrade, firstAlreadyExisted) = await _repository.IngestTradeIdempotentAsync(request);

        // 3. Act: Simulate network retry sending identical payload
        var (retryTrade, retryAlreadyExisted) = await _repository.IngestTradeIdempotentAsync(request);

        // 4. Assert: Correctness guarantees
        Assert.False(firstAlreadyExisted, "First call should indicate new trade created");
        Assert.True(retryAlreadyExisted, "Second call should indicate trade already existed");
        Assert.Equal(firstTrade.TradeId, retryTrade.TradeId);

        var allTrades = await _repository.GetAllAsync();
        Assert.Single(allTrades); // Exactly 1 record in database, no double-spend!
    }

    [Fact]
    public async Task ReconcileTrade_WhenPriceDriftExceedsHalfPercent_FlagsDiscrepancy()
    {
        // 1. Arrange: Ingest initial order at 1.0000
        var request = new IngestTradeRequest(
            IdempotencyKey: "ORDER-SLIPPAGE-TEST",
            AccountId: "ACC-JOSHUA-2",
            Symbol: "USDJPY",
            Side: OrderSide.Buy,
            Quantity: 50000m,
            Price: 150.00m
        );

        var (trade, _) = await _repository.IngestTradeIdempotentAsync(request);

        // 2. Act: Broker report comes back at 152.00 (drifts ~1.33%, exceeding 0.5% tolerance)
        var brokerReport = new BrokerExecutionReport(
            ExternalTradeId: "BROKER-EXEC-8821",
            Symbol: "USDJPY",
            Side: OrderSide.Buy,
            Quantity: 50000m,
            ExecutedPrice: 152.00m,
            ExecutionTimeUtc: DateTime.UtcNow
        );

        var reconciled = await _reconciliationService.ReconcileTradeAsync(trade.TradeId, brokerReport);
        var updatedTrade = await _repository.GetByIdAsync(trade.TradeId);

        // 3. Assert
        Assert.False(reconciled);
        Assert.NotNull(updatedTrade);
        Assert.Equal(TradeStatus.Discrepancy, updatedTrade.Status);
        Assert.Contains("Slippage exceeded 0.5%", updatedTrade.DiscrepancyReason);

        // Verify audit log exists for this discrepancy
        var logs = await _repository.GetAuditLogsAsync(trade.TradeId);
        Assert.Contains(logs, l => l.Action == "STATUS_CHANGED_DISCREPANCY");
    }

    [Fact]
    public async Task ReconcileTrade_WhenExecutionMatchesWithinTolerance_MarksReconciled()
    {
        // 1. Arrange
        var request = new IngestTradeRequest(
            IdempotencyKey: "ORDER-MATCH-TEST",
            AccountId: "ACC-JOSHUA-3",
            Symbol: "GBPUSD",
            Side: OrderSide.Sell,
            Quantity: 20000m,
            Price: 1.3000m
        );

        var (trade, _) = await _repository.IngestTradeIdempotentAsync(request);

        // 2. Act: Broker report at 1.3002 (well within 0.5% tolerance)
        var brokerReport = new BrokerExecutionReport(
            ExternalTradeId: "BROKER-EXEC-4410",
            Symbol: "GBPUSD",
            Side: OrderSide.Sell,
            Quantity: 20000m,
            ExecutedPrice: 1.3002m,
            ExecutionTimeUtc: DateTime.UtcNow
        );

        var reconciled = await _reconciliationService.ReconcileTradeAsync(trade.TradeId, brokerReport);
        var updatedTrade = await _repository.GetByIdAsync(trade.TradeId);

        // 3. Assert
        Assert.True(reconciled);
        Assert.NotNull(updatedTrade);
        Assert.Equal(TradeStatus.Reconciled, updatedTrade.Status);
    }
}

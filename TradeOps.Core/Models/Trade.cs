namespace TradeOps.Core.Models;

public enum OrderSide
{
    Buy,
    Sell
}

public enum TradeStatus
{
    Pending,
    Executed,
    Reconciled,
    Discrepancy,
    Failed
}

public record Trade(
    Guid TradeId,
    string IdempotencyKey,
    string AccountId,
    string Symbol,
    OrderSide Side,
    decimal Quantity,
    decimal Price,
    TradeStatus Status,
    DateTime CreatedAtUtc,
    string? DiscrepancyReason = null
);

public record IngestTradeRequest(
    string IdempotencyKey,
    string AccountId,
    string Symbol,
    OrderSide Side,
    decimal Quantity,
    decimal Price
);

public record AuditLog(
    Guid Id,
    Guid TradeId,
    string Action,
    string Details,
    DateTime TimestampUtc
);

public record BrokerExecutionReport(
    string ExternalTradeId,
    string Symbol,
    OrderSide Side,
    decimal Quantity,
    decimal ExecutedPrice,
    DateTime ExecutionTimeUtc
);
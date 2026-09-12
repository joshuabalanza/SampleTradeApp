using TradeOps.Core.Models;

namespace TradeOps.Core.Data;

// Mutable EF Core persistence model for the "trades" table (mapped separately from the immutable Trade domain record).
public class TradeEntity
{
    public Guid TradeId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public TradeStatus Status { get; set; }
    public string? DiscrepancyReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

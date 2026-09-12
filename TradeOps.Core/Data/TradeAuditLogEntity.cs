namespace TradeOps.Core.Data;

// Mutable EF Core persistence model for the append-only "trade_audit_logs" table.
public class TradeAuditLogEntity
{
    public long AuditId { get; set; }
    public Guid TradeId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

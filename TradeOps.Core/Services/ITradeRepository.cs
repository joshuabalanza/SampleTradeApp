namespace TradeOps.Core.Services;

using TradeOps.Core.Models;

public interface ITradeRepository
{
    // Returns the Trade and a boolean flag: true if it already existed (idempotent replay), false if newly created
    Task<(Trade Trade, bool AlreadyExisted)> IngestTradeIdempotentAsync(IngestTradeRequest request);
    Task<Trade?> GetByIdAsync(Guid tradeId);
    Task<IEnumerable<Trade>> GetAllAsync();
    Task UpdateStatusAsync(Guid tradeId, TradeStatus status, string? discrepancyReason = null);
    Task AddAuditLogAsync(Guid tradeId, string action, string details);
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Guid tradeId);
}
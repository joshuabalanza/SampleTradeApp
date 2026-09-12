using System.Collections.Concurrent;
using TradeOps.Core.Models;

namespace TradeOps.Core.Services;

public class InMemoryTradeRepository : ITradeRepository
{
    // Primary storage: TradeId -> Trade
    private readonly ConcurrentDictionary<Guid, Trade> _trades = new();

    // Idempotency index: IdempotencyKey -> TradeId (emulating UNIQUE INDEX in PostgreSQL)
    private readonly ConcurrentDictionary<string, Guid> _idempotencyIndex = new();

    // Append-only audit log stream
    private readonly ConcurrentBag<AuditLog> _auditLogs = new();

    // Concurrency lock for atomic read-then-write check
    private readonly object _syncLock = new();

    public Task<(Trade Trade, bool AlreadyExisted)> IngestTradeIdempotentAsync(IngestTradeRequest request)
    {
        lock (_syncLock)
        {
            // 1. Check if this exact idempotency key was already received
            if (_idempotencyIndex.TryGetValue(request.IdempotencyKey, out var existingTradeId))
            {
                var existingTrade = _trades[existingTradeId];
                return Task.FromResult((existingTrade, true));
            }

            // 2. Newly received trade: create new record
            var newTradeId = Guid.NewGuid();
            var newTrade = new Trade(
                TradeId: newTradeId,
                IdempotencyKey: request.IdempotencyKey,
                AccountId: request.AccountId,
                Symbol: request.Symbol,
                Side: request.Side,
                Quantity: request.Quantity,
                Price: request.Price,
                Status: TradeStatus.Pending,
                CreatedAtUtc: DateTime.UtcNow
            );

            // 3. Atomically store both the trade and the idempotency mapping
            _trades[newTradeId] = newTrade;
            _idempotencyIndex[request.IdempotencyKey] = newTradeId;

            // 4. Create immutable audit entry
            _auditLogs.Add(new AuditLog(
                Id: Guid.NewGuid(),
                TradeId: newTradeId,
                Action: "TRADE_INGESTED",
                Details: $"Order created: {request.Side} {request.Quantity} {request.Symbol} @ {request.Price}",
                TimestampUtc: DateTime.UtcNow
            ));

            return Task.FromResult((newTrade, false));
        }
    }

    public Task<Trade?> GetByIdAsync(Guid tradeId)
    {
        _trades.TryGetValue(tradeId, out var trade);
        return Task.FromResult(trade);
    }

    public Task<IEnumerable<Trade>> GetAllAsync()
    {
        var list = _trades.Values.OrderByDescending(t => t.CreatedAtUtc).ToList();
        return Task.FromResult<IEnumerable<Trade>>(list);
    }

    public Task UpdateStatusAsync(Guid tradeId, TradeStatus status, string? discrepancyReason = null)
    {
        lock (_syncLock)
        {
            if (_trades.TryGetValue(tradeId, out var existing))
            {
                var updated = existing with
                {
                    Status = status,
                    DiscrepancyReason = discrepancyReason
                };

                _trades[tradeId] = updated;

                _auditLogs.Add(new AuditLog(
                    Id: Guid.NewGuid(),
                    TradeId: tradeId,
                    Action: $"STATUS_CHANGED_{status.ToString().ToUpperInvariant()}",
                    Details: discrepancyReason ?? $"Trade status updated to {status}",
                    TimestampUtc: DateTime.UtcNow
                ));
            }
        }

        return Task.CompletedTask;
    }

    public Task AddAuditLogAsync(Guid tradeId, string action, string details)
    {
        _auditLogs.Add(new AuditLog(
            Id: Guid.NewGuid(),
            TradeId: tradeId,
            Action: action,
            Details: details,
            TimestampUtc: DateTime.UtcNow
        ));
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Guid tradeId)
    {
        var logs = _auditLogs
            .Where(a => a.TradeId == tradeId)
            .OrderBy(a => a.TimestampUtc)
            .ToList();
        return Task.FromResult<IEnumerable<AuditLog>>(logs);
    }
}

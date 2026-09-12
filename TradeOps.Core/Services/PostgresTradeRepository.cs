using Microsoft.EntityFrameworkCore;
using Npgsql;
using TradeOps.Core.Data;
using TradeOps.Core.Models;

namespace TradeOps.Core.Services;

public class PostgresTradeRepository : ITradeRepository
{
    private const string UniqueViolationSqlState = "23505";

    private readonly TradeOpsDbContext _db;

    public PostgresTradeRepository(TradeOpsDbContext db)
    {
        _db = db;
    }

    public async Task<(Trade Trade, bool AlreadyExisted)> IngestTradeIdempotentAsync(IngestTradeRequest request)
    {
        // 1. Check if idempotency key already exists
        var existing = await _db.Trades.AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey);
        if (existing is not null)
        {
            return (ToDomain(existing), true);
        }

        // 2. Insert new trade
        var entity = new TradeEntity
        {
            IdempotencyKey = request.IdempotencyKey,
            AccountId = request.AccountId,
            Symbol = request.Symbol,
            Side = request.Side,
            Quantity = request.Quantity,
            Price = request.Price,
            Status = TradeStatus.Pending
        };

        _db.Trades.Add(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            // Concurrent request won the race on the idempotency key: return the winner's row instead of duplicating.
            _db.Entry(entity).State = EntityState.Detached;
            var raced = await _db.Trades.AsNoTracking()
                .FirstAsync(t => t.IdempotencyKey == request.IdempotencyKey);
            return (ToDomain(raced), true);
        }

        return (ToDomain(entity), false);
    }

    public async Task<Trade?> GetByIdAsync(Guid tradeId)
    {
        var entity = await _db.Trades.AsNoTracking().FirstOrDefaultAsync(t => t.TradeId == tradeId);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<Trade>> GetAllAsync()
    {
        var entities = await _db.Trades.AsNoTracking()
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(100)
            .ToListAsync();

        return entities.Select(ToDomain);
    }

    public async Task UpdateStatusAsync(Guid tradeId, TradeStatus status, string? discrepancyReason = null)
    {
        var entity = await _db.Trades.FirstOrDefaultAsync(t => t.TradeId == tradeId);
        if (entity is null)
        {
            return;
        }

        entity.Status = status;
        entity.DiscrepancyReason = discrepancyReason;

        await _db.SaveChangesAsync();
    }

    public async Task AddAuditLogAsync(Guid tradeId, string action, string details)
    {
        _db.AuditLogs.Add(new TradeAuditLogEntity
        {
            TradeId = tradeId,
            Action = action,
            Details = details
        });

        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Guid tradeId)
    {
        var entities = await _db.AuditLogs.AsNoTracking()
            .Where(a => a.TradeId == tradeId)
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync();

        return entities.Select(e => new AuditLog(
            Id: Guid.NewGuid(),
            TradeId: e.TradeId,
            Action: e.Action,
            Details: e.Details ?? "",
            TimestampUtc: e.CreatedAtUtc
        ));
    }

    private static Trade ToDomain(TradeEntity entity) => new(
        TradeId: entity.TradeId,
        IdempotencyKey: entity.IdempotencyKey,
        AccountId: entity.AccountId,
        Symbol: entity.Symbol,
        Side: entity.Side,
        Quantity: entity.Quantity,
        Price: entity.Price,
        Status: entity.Status,
        CreatedAtUtc: entity.CreatedAtUtc,
        DiscrepancyReason: entity.DiscrepancyReason
    );
}

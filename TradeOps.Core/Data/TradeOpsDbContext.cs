using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;
using TradeOps.Core.Models;

namespace TradeOps.Core.Data;

public class TradeOpsDbContext : DbContext
{
    public TradeOpsDbContext(DbContextOptions<TradeOpsDbContext> options) : base(options)
    {
    }

    public DbSet<TradeEntity> Trades => Set<TradeEntity>();
    public DbSet<TradeAuditLogEntity> AuditLogs => Set<TradeAuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enum labels ('Buy', 'Pending', ...) match the C# names exactly, so disable snake_case translation.
        var nameTranslator = new NpgsqlNullNameTranslator();
        modelBuilder.HasPostgresEnum<OrderSide>("order_side", nameTranslator: nameTranslator);
        modelBuilder.HasPostgresEnum<TradeStatus>("trade_status", nameTranslator: nameTranslator);

        modelBuilder.Entity<TradeEntity>(entity =>
        {
            entity.ToTable("trades");
            entity.HasKey(t => t.TradeId);

            entity.Property(t => t.TradeId).HasColumnName("trade_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(t => t.IdempotencyKey).HasColumnName("idempotency_key");
            entity.Property(t => t.AccountId).HasColumnName("account_id");
            entity.Property(t => t.Symbol).HasColumnName("symbol");
            entity.Property(t => t.Side).HasColumnName("side").HasColumnType("order_side");
            entity.Property(t => t.Quantity).HasColumnName("quantity");
            entity.Property(t => t.Price).HasColumnName("price");
            entity.Property(t => t.Status).HasColumnName("status").HasColumnType("trade_status");
            entity.Property(t => t.DiscrepancyReason).HasColumnName("discrepancy_reason");
            entity.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("clock_timestamp()");

            entity.HasIndex(t => t.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<TradeAuditLogEntity>(entity =>
        {
            entity.ToTable("trade_audit_logs");
            entity.HasKey(a => a.AuditId);

            entity.Property(a => a.AuditId).HasColumnName("audit_id");
            entity.Property(a => a.TradeId).HasColumnName("trade_id");
            entity.Property(a => a.Action).HasColumnName("action");
            entity.Property(a => a.Details).HasColumnName("details");
            entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc").HasDefaultValueSql("clock_timestamp()");

            entity.HasIndex(a => new { a.TradeId, a.CreatedAtUtc });
        });
    }
}

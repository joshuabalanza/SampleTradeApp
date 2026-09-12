using System.Data;
using Npgsql;
using TradeOps.Core.Models;

namespace TradeOps.Core.Services;

public class PostgresTradeRepository : ITradeRepository
{
    private readonly string _connectionString;

    public PostgresTradeRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<(Trade Trade, bool AlreadyExisted)> IngestTradeIdempotentAsync(IngestTradeRequest request)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        // 1. Check if idempotency key already exists
        const string checkSql = @"SELECT trade_id, idempotency_key, account_id, symbol, side::text, quantity, price, status::text, created_at_utc, discrepancy_reason 
                                  FROM trades WHERE idempotency_key = @key LIMIT 1;";
        await using (var checkCmd = new NpgsqlCommand(checkSql, connection))
        {
            checkCmd.Parameters.AddWithValue("key", request.IdempotencyKey);
            await using var reader = await checkCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var existing = ReadTrade(reader);
                return (existing, true);
            }
        }

        // 2. Insert new trade atomically
        const string insertSql = @"
            INSERT INTO trades (idempotency_key, account_id, symbol, side, quantity, price, status)
            VALUES (@key, @account, @symbol, @side::order_side, @qty, @price, 'Pending'::trade_status)
            RETURNING trade_id, idempotency_key, account_id, symbol, side::text, quantity, price, status::text, created_at_utc, discrepancy_reason;";

        await using var insertCmd = new NpgsqlCommand(insertSql, connection);
        insertCmd.Parameters.AddWithValue("key", request.IdempotencyKey);
        insertCmd.Parameters.AddWithValue("account", request.AccountId);
        insertCmd.Parameters.AddWithValue("symbol", request.Symbol);
        insertCmd.Parameters.AddWithValue("side", request.Side.ToString());
        insertCmd.Parameters.AddWithValue("qty", request.Quantity);
        insertCmd.Parameters.AddWithValue("price", request.Price);

        await using var insertReader = await insertCmd.ExecuteReaderAsync();
        await insertReader.ReadAsync();
        var newTrade = ReadTrade(insertReader);

        return (newTrade, false);
    }

    public async Task<Trade?> GetByIdAsync(Guid tradeId)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        const string sql = @"SELECT trade_id, idempotency_key, account_id, symbol, side::text, quantity, price, status::text, created_at_utc, discrepancy_reason 
                             FROM trades WHERE trade_id = @id LIMIT 1;";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("id", tradeId);

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadTrade(reader) : null;
    }

    public async Task<IEnumerable<Trade>> GetAllAsync()
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        const string sql = @"SELECT trade_id, idempotency_key, account_id, symbol, side::text, quantity, price, status::text, created_at_utc, discrepancy_reason 
                             FROM trades ORDER BY created_at_utc DESC LIMIT 100;";

        await using var cmd = new NpgsqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        var trades = new List<Trade>();
        while (await reader.ReadAsync())
        {
            trades.Add(ReadTrade(reader));
        }
        return trades;
    }

    public async Task UpdateStatusAsync(Guid tradeId, TradeStatus status, string? discrepancyReason = null)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        const string sql = @"UPDATE trades 
                             SET status = @status::trade_status, discrepancy_reason = @reason 
                             WHERE trade_id = @id;";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("status", status.ToString());
        cmd.Parameters.AddWithValue("reason", (object?)discrepancyReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("id", tradeId);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddAuditLogAsync(Guid tradeId, string action, string details)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        const string sql = @"INSERT INTO trade_audit_logs (trade_id, action, details) 
                             VALUES (@tradeId, @action, @details);";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("tradeId", tradeId);
        cmd.Parameters.AddWithValue("action", action);
        cmd.Parameters.AddWithValue("details", details);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Guid tradeId)
    {
        await using var dataSource = NpgsqlDataSource.Create(_connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        const string sql = @"SELECT audit_id, trade_id, action, details, created_at_utc 
                             FROM trade_audit_logs 
                             WHERE trade_id = @tradeId ORDER BY created_at_utc ASC;";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("tradeId", tradeId);

        await using var reader = await cmd.ExecuteReaderAsync();
        var logs = new List<AuditLog>();
        while (await reader.ReadAsync())
        {
            logs.Add(new AuditLog(
                Id: Guid.NewGuid(),
                TradeId: reader.GetGuid(1),
                Action: reader.GetString(2),
                Details: reader.IsDBNull(3) ? "" : reader.GetString(3),
                TimestampUtc: reader.GetDateTime(4)
            ));
        }
        return logs;
    }

    private static Trade ReadTrade(NpgsqlDataReader reader)
    {
        return new Trade(
            TradeId: reader.GetGuid(0),
            IdempotencyKey: reader.GetString(1),
            AccountId: reader.GetString(2),
            Symbol: reader.GetString(3),
            Side: Enum.Parse<OrderSide>(reader.GetString(4), true),
            Quantity: reader.GetDecimal(5),
            Price: reader.GetDecimal(6),
            Status: Enum.Parse<TradeStatus>(reader.GetString(7), true),
            CreatedAtUtc: reader.GetDateTime(8),
            DiscrepancyReason: reader.IsDBNull(9) ? null : reader.GetString(9)
        );
    }
}

using Microsoft.EntityFrameworkCore;
using Npgsql;
using TradeOps.Core.Data;
using TradeOps.Core.Models;

namespace TradeOps.Core.Services;

public class PostgresUserService : IUserService
{
    private const string UniqueViolationSqlState = "23505";
    private readonly TradeOpsDbContext _db;

    public PostgresUserService(TradeOpsDbContext db)
    {
        _db = db;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request)
    {
        var usernameTrimmed = request.Username.Trim();
        var existing = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == usernameTrimmed.ToLower());

        if (existing is not null)
        {
            throw new InvalidOperationException("Username is already taken.");
        }

        var userEntity = new UserEntity
        {
            Username = usernameTrimmed,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? usernameTrimmed : request.DisplayName.Trim(),
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Trader" : request.Role.Trim(),
            AccountId = string.IsNullOrWhiteSpace(request.AccountId) ? $"ACC-{usernameTrimmed.ToUpper()}" : request.AccountId.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Users.Add(userEntity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            throw new InvalidOperationException("Username is already taken.");
        }

        return ToDto(userEntity);
    }

    public async Task<UserDto> LoginAsync(LoginRequest request)
    {
        var usernameTrimmed = request.Username.Trim();
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == usernameTrimmed.ToLower());

        if (user is null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        return ToDto(user);
    }

    public async Task<UserDto?> GetByUsernameAsync(string username)
    {
        var usernameTrimmed = username.Trim();
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == usernameTrimmed.ToLower());

        return user is null ? null : ToDto(user);
    }

    private static UserDto ToDto(UserEntity user) => new(
        UserId: user.UserId,
        Username: user.Username,
        DisplayName: user.DisplayName,
        Role: user.Role,
        AccountId: user.AccountId,
        CreatedAtUtc: user.CreatedAtUtc
    );
}

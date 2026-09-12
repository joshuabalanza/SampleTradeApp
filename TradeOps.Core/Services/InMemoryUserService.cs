using System.Collections.Concurrent;
using TradeOps.Core.Models;

namespace TradeOps.Core.Services;

public class InMemoryUserService : IUserService
{
    private readonly ConcurrentDictionary<string, UserDtoWithHash> _users = new(StringComparer.OrdinalIgnoreCase);

    private record UserDtoWithHash(UserDto User, string PasswordHash);

    public InMemoryUserService()
    {
        // Seed default demo accounts
        SeedUser("trader1", "demo123", "Joshua Kim Balanza", "Trader", "ACC-JOSHUA-101");
        SeedUser("ops1", "demo123", "Back-Office Operations", "Operations", "ACC-OPS-001");
    }

    private void SeedUser(string username, string password, string displayName, string role, string accountId)
    {
        var dto = new UserDto(
            UserId: Guid.NewGuid(),
            Username: username,
            DisplayName: displayName,
            Role: role,
            AccountId: accountId,
            CreatedAtUtc: DateTime.UtcNow
        );
        _users[username] = new UserDtoWithHash(dto, PasswordHasher.HashPassword(password));
    }

    public Task<UserDto> RegisterAsync(RegisterRequest request)
    {
        var usernameTrimmed = request.Username.Trim();

        if (_users.ContainsKey(usernameTrimmed))
        {
            throw new InvalidOperationException("Username is already taken.");
        }

        var dto = new UserDto(
            UserId: Guid.NewGuid(),
            Username: usernameTrimmed,
            DisplayName: string.IsNullOrWhiteSpace(request.DisplayName) ? usernameTrimmed : request.DisplayName.Trim(),
            Role: string.IsNullOrWhiteSpace(request.Role) ? "Trader" : request.Role.Trim(),
            AccountId: string.IsNullOrWhiteSpace(request.AccountId) ? $"ACC-{usernameTrimmed.ToUpper()}" : request.AccountId.Trim(),
            CreatedAtUtc: DateTime.UtcNow
        );

        var record = new UserDtoWithHash(dto, PasswordHasher.HashPassword(request.Password));

        if (!_users.TryAdd(usernameTrimmed, record))
        {
            throw new InvalidOperationException("Username is already taken.");
        }

        return Task.FromResult(dto);
    }

    public Task<UserDto> LoginAsync(LoginRequest request)
    {
        var usernameTrimmed = request.Username.Trim();

        if (!_users.TryGetValue(usernameTrimmed, out var record) ||
            !PasswordHasher.VerifyPassword(request.Password, record.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        return Task.FromResult(record.User);
    }

    public Task<UserDto?> GetByUsernameAsync(string username)
    {
        if (_users.TryGetValue(username.Trim(), out var record))
        {
            return Task.FromResult<UserDto?>(record.User);
        }

        return Task.FromResult<UserDto?>(null);
    }
}

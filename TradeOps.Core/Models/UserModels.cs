namespace TradeOps.Core.Models;

public record UserDto(
    Guid UserId,
    string Username,
    string DisplayName,
    string Role,
    string AccountId,
    DateTime CreatedAtUtc
);

public record RegisterRequest(
    string Username,
    string Password,
    string DisplayName,
    string? Role = null,
    string? AccountId = null
);

public record LoginRequest(
    string Username,
    string Password
);

public record AuthResponse(
    UserDto User,
    string Message
);

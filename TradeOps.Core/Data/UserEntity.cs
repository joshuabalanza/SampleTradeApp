namespace TradeOps.Core.Data;

public class UserEntity
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Trader";
    public string AccountId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

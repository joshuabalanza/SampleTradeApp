using TradeOps.Core.Models;
using TradeOps.Core.Services;
using Xunit;

namespace TradeOps.Tests;

public class UserAuthTests
{
    private readonly InMemoryUserService _userService = new();

    [Fact]
    public async Task RegisterAsync_WhenNewUser_CreatesUserWithHashedPassword()
    {
        var request = new RegisterRequest(
            Username: "newtrader",
            Password: "SecurePassword123!",
            DisplayName: "New Trader",
            Role: "Trader",
            AccountId: "ACC-NEW-1"
        );

        var user = await _userService.RegisterAsync(request);

        Assert.NotNull(user);
        Assert.Equal("newtrader", user.Username);
        Assert.Equal("New Trader", user.DisplayName);
        Assert.Equal("Trader", user.Role);
        Assert.Equal("ACC-NEW-1", user.AccountId);
    }

    [Fact]
    public async Task RegisterAsync_WhenDuplicateUsername_ThrowsInvalidOperationException()
    {
        var request = new RegisterRequest("trader1", "password123", "Trader 1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsValid_ReturnsUserDto()
    {
        var request = new RegisterRequest("john_doe", "MyPass999", "John Doe");
        await _userService.RegisterAsync(request);

        var user = await _userService.LoginAsync(new LoginRequest("john_doe", "MyPass999"));

        Assert.NotNull(user);
        Assert.Equal("john_doe", user.Username);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIncorrect_ThrowsUnauthorizedAccessException()
    {
        var request = new RegisterRequest("jane_doe", "CorrectPass", "Jane Doe");
        await _userService.RegisterAsync(request);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _userService.LoginAsync(new LoginRequest("jane_doe", "WrongPass")));
    }
}

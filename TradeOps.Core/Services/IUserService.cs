using TradeOps.Core.Models;

namespace TradeOps.Core.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterRequest request);
    Task<UserDto> LoginAsync(LoginRequest request);
    Task<UserDto?> GetByUsernameAsync(string username);
}

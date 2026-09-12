using Microsoft.AspNetCore.Mvc;
using TradeOps.Core.Models;
using TradeOps.Core.Services;

namespace TradeOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Username and Password are required." });
        }

        if (request.Password.Length < 6)
        {
            return BadRequest(new { error = "Password must be at least 6 characters long." });
        }

        try
        {
            var user = await _userService.RegisterAsync(request);
            _logger.LogInformation("User registered successfully: {Username}", user.Username);
            return Ok(new AuthResponse(user, "Registration successful."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Username}", request.Username);
            return StatusCode(500, new { error = "An error occurred during registration." });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Username and Password are required." });
        }

        try
        {
            var user = await _userService.LoginAsync(request);
            _logger.LogInformation("User logged in successfully: {Username}", user.Username);
            return Ok(new AuthResponse(user, "Login successful."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Username}", request.Username);
            return StatusCode(500, new { error = "An error occurred during login." });
        }
    }

    [HttpGet("me/{username}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(string username)
    {
        var user = await _userService.GetByUsernameAsync(username);
        if (user is null) return NotFound(new { error = "User not found." });
        return Ok(user);
    }
}

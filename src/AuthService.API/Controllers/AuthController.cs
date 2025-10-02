using AuthService.API.Models;
using AuthService.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AuthService.API.Controllers
{
    //[ApiController]
    //[Route("api/v{version:apiVersion}/[controller]")]
    //[ApiVersion("1.0")]
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;

        // Simulated in-memory users (replace with repository in production)
        private static readonly Dictionary<string, (string Password, List<string> Roles)> _users = new()
        {
            { "alice", ("Password123!", new List<string> { "TenantAdmin", "TenantOperator" }) },
            { "bob", ("Password123!", new List<string> { "TenantAnalyst" }) }
        };

        // In-memory refresh token store (replace with DB/Redis)
        private static readonly Dictionary<string, (string RefreshToken, DateTime Expiry)> _refreshTokens = new();

        public AuthController(ITokenService tokenService) => _tokenService = tokenService;

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (!_users.TryGetValue(request.Username, out var userInfo) || userInfo.Password != request.Password)
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid credentials", "INVALID_CREDENTIALS", 401));

            // Generate Access Token
            var accessToken = await _tokenService.GenerateToken(request.Username, request.TenantId, userInfo.Roles, cancellationToken);
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);

            // Generate Refresh Token
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            // Save refresh token in memory (replace with DB/Redis)
            _refreshTokens[refreshToken] = (request.Username, refreshTokenExpiry);

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiry = accessTokenExpiry,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = refreshTokenExpiry
            };

            return Ok(ApiResponse<LoginResponse>.Ok(response));
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            if (!_refreshTokens.ContainsKey(request.RefreshToken))
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid refresh token", "INVALID_REFRESH_TOKEN", 401));

            var (username, expiryAt) = _refreshTokens[request.RefreshToken];
            if (expiryAt < DateTime.UtcNow)
            {
                _refreshTokens.Remove(request.RefreshToken);
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Refresh token expired", "EXPIRED_REFRESH_TOKEN", 401));
            }
            
            var userInfo = _users[username];
            var tenantId = "tenant1"; // Should be retrieved from DB per user

            //var newAccessToken = await _tokenService.GenerateToken(tokenInfo.RefreshToken, tenantId, userInfo.Roles, cancellationToken);
            var newAccessToken = await _tokenService.GenerateToken(username, tenantId, userInfo.Roles, cancellationToken);
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);

            var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            _refreshTokens.Remove(request.RefreshToken);
            _refreshTokens[newRefreshToken] = (newRefreshToken, refreshTokenExpiry);

            var response = new LoginResponse
            {
                AccessToken = newAccessToken,
                AccessTokenExpiry = accessTokenExpiry,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiry = refreshTokenExpiry
            };

            return Ok(ApiResponse<LoginResponse>.Ok(response));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(millisecondsDelay: 0, cancellationToken);

            if (_refreshTokens.ContainsKey(request.RefreshToken))
                _refreshTokens.Remove(request.RefreshToken);

            return Ok(ApiResponse<string>.Ok("Logged out successfully"));
        }

        // TODO: not working yet
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> Me(CancellationToken cancellationToken)
        {
            await Task.Delay(millisecondsDelay: 0, cancellationToken);

            //var username = User.Identity?.Name ?? "Unknown";
            var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            var tenantId = User.Claims.FirstOrDefault(c => c.Type == "TenantId")?.Value ?? "Unknown";
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            return Ok(ApiResponse<object>.Ok(new { username, tenantId, roles }));
        }
    }
}

using AuthService.API.Models;
using AuthService.API.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using AuthService.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AuthService.API.Controllers
{
    [Authorize(Policy = "EndpointRolesPolicy")]
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        // In-memory refresh token store (replace with DB/Redis in production)
        private static readonly Dictionary<string, (Guid UserId, DateTime Expiry)> _refreshTokens = new();

        public AuthController(ITokenService tokenService, IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _tokenService = tokenService;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<string>>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            // Check for existing user
            if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
                return Conflict(ApiResponse<string>.Fail("Email already registered", "EMAIL_EXISTS", 409));

            if (await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
                return Conflict(ApiResponse<string>.Fail("Username already taken", "USERNAME_EXISTS", 409));

            var passwordHash = _passwordHasher.HashPassword(request.Password);

            var tenantId = string.IsNullOrEmpty(request.TenantId)
                ? throw new MissingFieldException("TenantId is required")
                : Guid.Parse(request.TenantId);

            var user = new User(request.Email, request.Username, request.PhoneNumber, passwordHash, tenantId);

            await _userRepository.AddAsync(user, cancellationToken);

            return Ok(ApiResponse<string>.Ok("User registered successfully"));
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByLoginIdentifierAsync(request.LoginIdentifier, cancellationToken);
            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid credentials", "INVALID_CREDENTIALS", 401));

            var accessToken = await _tokenService.GenerateToken(user, cancellationToken);
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);

            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            _refreshTokens[refreshToken] = (user.Id, refreshTokenExpiry);

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
            if (!_refreshTokens.TryGetValue(request.RefreshToken, out var tokenInfo))
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid refresh token", "INVALID_REFRESH_TOKEN", 401));

            if (tokenInfo.Expiry < DateTime.UtcNow)
            {
                _refreshTokens.Remove(request.RefreshToken);
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Refresh token expired", "EXPIRED_REFRESH_TOKEN", 401));
            }

            var user = await _userRepository.GetByIdAsync(tokenInfo.UserId, cancellationToken);
            if (user == null)
            {
                _refreshTokens.Remove(request.RefreshToken);
                return Unauthorized(ApiResponse<LoginResponse>.Fail("User not found", "USER_NOT_FOUND", 401));
            }

            var roles = user.Roles.Select(r => r.RoleType.ToString()).ToList();

            var newAccessToken = await _tokenService.GenerateToken(user, cancellationToken);
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);

            var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            _refreshTokens.Remove(request.RefreshToken);
            _refreshTokens[newRefreshToken] = (user.Id, refreshTokenExpiry);

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
        public async Task<ActionResult<ApiResponse<string>>> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(0, cancellationToken);

            if (_refreshTokens.ContainsKey(request.RefreshToken))
                _refreshTokens.Remove(request.RefreshToken);

            return Ok(ApiResponse<string>.Ok("Logged out successfully"));
        }

        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<object>>> Me(CancellationToken cancellationToken)
        {
            await Task.Delay(0, cancellationToken);

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                              ?? User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(ApiResponse<object>.Fail("Invalid user claims", "INVALID_CLAIMS", 401));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return Unauthorized(ApiResponse<object>.Fail("User not found", "USER_NOT_FOUND", 401));

            var roles = user.Roles.Select(r => r.RoleType.ToString()).ToList();

            return Ok(ApiResponse<object>.Ok(new
            {
                user.Id,
                user.Email,
                user.Username,
                user.PhoneNumber,
                user.TenantId,
                Roles = roles
            }));
        }
    }
}

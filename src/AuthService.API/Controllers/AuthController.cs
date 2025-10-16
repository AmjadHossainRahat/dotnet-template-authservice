using AuthService.API.Models;
using AuthService.API.Services;
using AuthService.Application.Mediator;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Caching;
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
        private readonly ICacheService _cache;

        public AuthController(
            ITokenService tokenService,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            ICacheService cache)
        {
            _tokenService = tokenService;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _cache = cache;
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
            var cachedUserId = await _cache.GetAsync<string>($"users_id:{request.LoginIdentifier}");
            var cacheKey = string.Empty;
            LoginResponse? cachedResponse = null;

            if (!string.IsNullOrEmpty(cachedUserId))
            {
                cacheKey = $"user_tokens:{cachedUserId}";
                cachedResponse = await _cache.GetAsync<LoginResponse>(cacheKey);
            }

            if (cachedResponse != null)
            {
                Console.WriteLine($"Login for {request.LoginIdentifier}: serving from cache");
                return Ok(ApiResponse<LoginResponse>.Ok(cachedResponse));
            }

            var user = await _userRepository.GetByLoginIdentifierAsync(request.LoginIdentifier, cancellationToken);
            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid credentials", "INVALID_CREDENTIALS", 401));

            cacheKey = $"user_tokens:{user.Id}";

            // Generate new tokens
            var accessToken = await _tokenService.GenerateToken(user, cancellationToken);
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);

            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiry = accessTokenExpiry,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = refreshTokenExpiry
            };

            // Cache with TTL same as refresh token
            await _cache.SetAsync<LoginResponse>(cacheKey, response, refreshTokenExpiry - DateTime.UtcNow);
            await _cache.SetAsync<string>($"users_id:{request.LoginIdentifier}", user.Id.ToString(), refreshTokenExpiry - DateTime.UtcNow);
            await _cache.SetAsync<LoginResponse>($"refresh_token:{refreshToken}", response, refreshTokenExpiry - DateTime.UtcNow + TimeSpan.FromMinutes(60));

            return Ok(ApiResponse<LoginResponse>.Ok(response));
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            // Retrieve cached token info by refresh token
            var tokenCacheKey = $"refresh_token:{request.RefreshToken}";
            var cachedResponse = await _cache.GetAsync<LoginResponse>(tokenCacheKey);

            if (cachedResponse != null && cachedResponse.RefreshTokenExpiry > DateTime.UtcNow)
            {
                Console.WriteLine($"RefreshToken for {cachedResponse.RefreshToken}: serving from cache");
                return Ok(ApiResponse<LoginResponse>.Ok(cachedResponse));
            }
            else if(cachedResponse == null)
            {
                return Unauthorized(ApiResponse<LoginResponse>.Fail("RefreshToken is not valid, please login again", "INVALID_REFRESH_TOKEN", 401));
            }
            
            // refresh token valid but expired

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                                      ?? User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(ApiResponse<object>.Fail("Invalid user claims", "INVALID_CLAIMS", 401));

            if (userId == Guid.Empty)
                return Unauthorized(ApiResponse<LoginResponse>.Fail("User not found", "USER_NOT_FOUND", 401));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return Unauthorized(ApiResponse<LoginResponse>.Fail("User not found", "USER_NOT_FOUND", 401));

            var newAccessToken = await _tokenService.GenerateToken(user, cancellationToken);
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(60);
            var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            var response = new LoginResponse
            {
                AccessToken = newAccessToken,
                AccessTokenExpiry = accessTokenExpiry,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiry = refreshTokenExpiry
            };

            await _cache.RemoveAsync(tokenCacheKey);

            // Cache with TTL
            await _cache.SetAsync($"user_tokens:{user.Id}", response, refreshTokenExpiry - DateTime.UtcNow);
            await _cache.SetAsync($"refresh_token:{newRefreshToken}", response, refreshTokenExpiry - DateTime.UtcNow + +TimeSpan.FromMinutes(60));

            return Ok(ApiResponse<LoginResponse>.Ok(response));
        }

        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse<string>>> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                              ?? User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(ApiResponse<object>.Fail("Invalid user claims", "INVALID_CLAIMS", 401));

            var tokenCacheKey = $"refresh_token:{request.RefreshToken}";
            var cachedResponse = await _cache.GetAsync<LoginResponse>(tokenCacheKey);
            if (cachedResponse == null)
            {
                return Unauthorized(ApiResponse<LoginResponse>.Fail("RefreshToken is not valid or already expired/Logged out", "INVALID_REFRESH_TOKEN", 401));
            }

            await _cache.RemoveAsync($"user_tokens:{userId}");
            await _cache.RemoveAsync($"refresh_token:{request.RefreshToken}");

            return Ok(ApiResponse<string>.Ok("Logged out successfully"));
        }

        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<object>>> Me(CancellationToken cancellationToken)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                              ?? User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(ApiResponse<object>.Fail("Invalid user claims", "INVALID_CLAIMS", 401));

            var cacheKey = $"me:{userId}";
            var cachedResponse = await _cache.GetAsync<object>(cacheKey);
            if (cachedResponse != null)
            {
                Console.WriteLine($"Me for {userId}: serving from cache");
                return Ok(ApiResponse<object>.Ok(cachedResponse));
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return Unauthorized(ApiResponse<object>.Fail("User not found", "USER_NOT_FOUND", 401));

            var roles = user.Roles.Select(r => r.RoleType.ToString()).ToList();

            var response = new
            {
                user.Id,
                user.Email,
                user.Username,
                user.PhoneNumber,
                user.TenantId,
                Roles = roles
            };

            await _cache.SetAsync<object>(cacheKey, response, TimeSpan.FromMinutes(60));

            return Ok(ApiResponse<object>.Ok(response));
        }
    }
}

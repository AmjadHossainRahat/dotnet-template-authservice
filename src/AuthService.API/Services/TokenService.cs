using AuthService.API.Settings;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;

namespace AuthService.API.Services
{
    public interface ITokenService
    {
        Task<string> GenerateToken(string userId, string tenantId, IEnumerable<string> roles, CancellationToken cancellationToken);
        Task<string> GenerateToken(string userId, string tenantId, IEnumerable<string> roles, int expiryMinutes, CancellationToken cancellationToken);
    }

    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly RSA _rsa;

        public TokenService(JwtSettings jwtSettings, RSA rsa)
        {
            _jwtSettings = jwtSettings;
            _rsa = rsa;
        }

        public static async Task<TokenService> CreateAsync(JwtSettings jwtSettings, CancellationToken cancellationToken)
        {
            var rsa = RSA.Create();
            var privateKey = await File.ReadAllTextAsync(jwtSettings.PrivateKeyPath, cancellationToken);
            rsa.ImportFromPem(privateKey);

            return new TokenService(jwtSettings, rsa);
        }

        public async Task<string> GenerateToken(string userId, string tenantId, IEnumerable<string> roles, int expiryMinutes, CancellationToken cancellationToken)
        {
            await Task.Delay(millisecondsDelay: 0, cancellationToken);
            var credentials = new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim("TenantId", tenantId)
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GenerateToken(string userId, string tenantId, IEnumerable<string> roles, CancellationToken cancellationToken)
        {
            await Task.Delay(millisecondsDelay: 0, cancellationToken);
            var credentials = new SigningCredentials(
                new RsaSecurityKey(_rsa),
                SecurityAlgorithms.RsaSha256
            );

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim("TenantId", tenantId)
            };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

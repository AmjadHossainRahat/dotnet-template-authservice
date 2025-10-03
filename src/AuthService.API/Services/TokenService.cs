using AuthService.API.Settings;
using AuthService.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AuthService.API.Services
{
    public interface ITokenService
    {
        Task<string> GenerateToken(User user, CancellationToken cancellationToken);
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

        public async Task<string> GenerateToken(User user, CancellationToken cancellationToken)
        {
            await Task.Delay(0, cancellationToken);

            var credentials = new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("Username", user.Username),
                new Claim("TenantId", user.TenantId.ToString())
            };
            if (!string.IsNullOrEmpty(user.PhoneNumber)) claims.Add(new Claim("PhoneNumber", user.PhoneNumber));

            claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.RoleType.ToString())));

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

using AuthService.API.Services;
using AuthService.API.Settings;
using AuthService.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AuthService.API.Tests.Services
{
    [TestFixture]
    public class TokenServiceTests
    {
        private JwtSettings _jwtSettings = null!;
        private RSA _rsa = null!;
        private TokenService _tokenService = null!;

        [SetUp]
        public void Setup()
        {
            // Setup RSA key for signing (pair)
            _rsa = RSA.Create();
            _rsa.KeySize = 2048;

            // Setup JwtSettings
            _jwtSettings = new JwtSettings
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpiryMinutes = 60
            };

            _tokenService = new TokenService(_jwtSettings, _rsa);
        }

        [Test]
        public void GenerateToken_ShouldReturnValidJwt_WithCorrectClaimsAndHeader()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var user = new User("alice@example.com", "alice", "1234567890", "hashedpassword", tenantId);
            var role1 = new Role(RoleEnum.TenantAdmin, tenantId);
            var role2 = new Role(RoleEnum.TenantOperator, tenantId);
            user.AssignRole(role1);
            user.AssignRole(role2);

            // Act
            var tokenString = _tokenService.GenerateToken(user, CancellationToken.None).GetAwaiter().GetResult();
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            // Assert - issuer/audience/claims/header/expiry
            Assert.That(token.Issuer, Is.EqualTo(_jwtSettings.Issuer));
            Assert.That(token.Audiences.First(), Is.EqualTo(_jwtSettings.Audience));
            Assert.That(token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value, Is.EqualTo(user.Id.ToString()));
            Assert.That(token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value, Is.EqualTo(user.Email));
            Assert.That(token.Claims.First(c => c.Type == "Username").Value, Is.EqualTo(user.Username));
            Assert.That(token.Claims.First(c => c.Type == "PhoneNumber").Value, Is.EqualTo(user.PhoneNumber));
            Assert.That(token.Claims.First(c => c.Type == "TenantId").Value, Is.EqualTo(user.TenantId.ToString()));

            // header algorithm is RS256 (RsaSha256)
            var algInHeader = token.Header["alg"]?.ToString();
            Assert.That(algInHeader, Is.EqualTo(SecurityAlgorithms.RsaSha256));

            // role claims
            var roleClaims = token.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            Assert.That(roleClaims, Does.Contain(RoleEnum.TenantAdmin.ToString()));
            Assert.That(roleClaims, Does.Contain(RoleEnum.TenantOperator.ToString()));

            // expiry
            Assert.That(token.ValidTo, Is.GreaterThan(DateTime.UtcNow));
            // within reasonable bounds (expiryMinutes)
            Assert.That(token.ValidTo, Is.LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes + 1)));
        }

        [Test]
        public void GenerateToken_ShouldNotIncludePhoneClaim_WhenPhoneIsNullOrEmpty()
        {
            // Arrange (no phone)
            var tenantId = Guid.NewGuid();
            var user = new User("bob@example.com", "bob", null, "hashedpassword", tenantId);
            var role = new Role(RoleEnum.TenantAnalyst, tenantId);
            user.AssignRole(role);

            // Act
            var tokenString = _tokenService.GenerateToken(user, CancellationToken.None).GetAwaiter().GetResult();
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            // Assert - phone claim absent
            Assert.That(token.Claims.Any(c => c.Type == "PhoneNumber"), Is.False);
            Assert.That(token.Claims.First(c => c.Type == "Username").Value, Is.EqualTo(user.Username));
            Assert.That(token.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Role).Value, Is.EqualTo(RoleEnum.TenantAnalyst.ToString()));
        }

        [Test]
        public void GenerateToken_ShouldIncludeAllAssignedRoles()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var user = new User("charlie@example.com", "charlie", string.Empty, "hashedpassword", tenantId);
            var roles = new[]
            {
                new Role(RoleEnum.TenantAdmin, tenantId),
                new Role(RoleEnum.TenantOperator, tenantId),
                new Role(RoleEnum.TenantAnalyst, tenantId)
            };
            foreach (var role in roles) user.AssignRole(role);

            // Act
            var tokenString = _tokenService.GenerateToken(user, CancellationToken.None).GetAwaiter().GetResult();
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            // Assert - all roles present
            var roleClaims = token.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                                         .Select(c => c.Value)
                                         .ToList();
            foreach (var role in roles)
            {
                Assert.That(roleClaims, Does.Contain(role.RoleType.ToString()));
            }
        }

        [Test]
        public void GeneratedToken_ShouldBeValid_WhenValidatedWithCorrespondingRsaKey()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var user = new User("val@example.com", "val", "000", "hashed", tenantId);
            user.AssignRole(new Role(RoleEnum.TenantAdmin, tenantId));

            var tokenString = _tokenService.GenerateToken(user, CancellationToken.None).GetAwaiter().GetResult();

            // Prepare validation params using the same RSA key pair (public part)
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new RsaSecurityKey(_rsa) // uses public portion from same RSA instance
            };

            var handler = new JwtSecurityTokenHandler();

            // Act
            var principal = handler.ValidateToken(tokenString, validationParameters, out var validatedToken);

            // Assert - principal & validated token not null
            Assert.That(principal, Is.Not.Null);
            Assert.That(validatedToken, Is.Not.Null);

            // Robust sub claim lookup (try common claim type names)
            var subClaim =
                principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub) ??
                principal.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier) ??
                principal.Claims.FirstOrDefault(c => c.Type == "sub");

            Assert.That(subClaim, Is.Not.Null, "sub (subject) claim not found in validated principal.");
            Assert.That(subClaim!.Value, Is.EqualTo(user.Id.ToString()));
        }


        [Test]
        public void ValidateToken_ShouldFail_WhenIssuerDoesNotMatch()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var user = new User("val2@example.com", "val2", "000", "hashed", tenantId);
            user.AssignRole(new Role(RoleEnum.TenantAdmin, tenantId));

            var tokenString = _tokenService.GenerateToken(user, CancellationToken.None).GetAwaiter().GetResult();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "WrongIssuer",
                IssuerSigningKey = new RsaSecurityKey(_rsa)
            };

            var handler = new JwtSecurityTokenHandler();

            // Act & Assert - wrong issuer should throw a SecurityTokenInvalidIssuerException
            Assert.Throws<Microsoft.IdentityModel.Tokens.SecurityTokenInvalidIssuerException>(() =>
            {
                handler.ValidateToken(tokenString, validationParameters, out var _);
            });
        }

        [Test]
        public void GenerateToken_ShouldHonorCancellationToken_WhenCancelledBeforeCall()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var user = new User("cancel@example.com", "cancel", "000", "hashed", tenantId);
            var role = new Role(RoleEnum.TenantAdmin, tenantId);
            user.AssignRole(role);

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            var ex = Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await _tokenService.GenerateToken(user, cts.Token));
        }


        [Test]
        public void GenerateToken_WithNullUser_ShouldThrow()
        {
            // Arrange
            User? user = null;

            // Act & Assert - current implementation accesses properties and will throw a NullReferenceException.
            // This test documents current behavior; we can change TokenService to throw ArgumentNullException if desired.
            Assert.Throws<NullReferenceException>(() =>
            {
                _tokenService.GenerateToken(user!, CancellationToken.None).GetAwaiter().GetResult();
            });
        }
    }
}

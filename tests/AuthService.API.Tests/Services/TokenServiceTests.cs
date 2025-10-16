using System.Security.Cryptography;
using AuthService.API.Services;
using AuthService.API.Settings;
using AuthService.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;

namespace AuthService.API.Tests.Services
{
    [TestFixture]
    public class TokenServiceTests
    {
        private JwtSettings _jwtSettings = null!;
        private TokenService _service = null!;

        [SetUp]
        public void Setup()
        {
            _jwtSettings = new JwtSettings
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpiryMinutes = 60,
            };
            var rsa = RSA.Create();
            _service = new TokenService(_jwtSettings, rsa);
        }

        // ------------------ GenerateToken Tests ------------------

        [Test]
        public async Task GenerateToken_ShouldReturnValidJwt_WhenUserIsValid()
        {
            var tenantId = Guid.NewGuid();
            var user = new User("test@example.com", "username", "1234567890", "hashed", tenantId);
            user.Roles.Add(new Role(RoleEnum.SystemAdmin, tenantId));

            var token = await _service.GenerateToken(user, CancellationToken.None);

            Assert.That(token, Is.Not.Null.Or.Empty);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            Assert.That(jwt.Issuer, Is.EqualTo(_jwtSettings.Issuer));
            Assert.That(jwt.Audiences.First(), Is.EqualTo(_jwtSettings.Audience));

            var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            Assert.That(subClaim?.Value, Is.EqualTo(user.Id.ToString()));

            var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == "roles");
            Assert.That(roleClaim?.Value, Is.EqualTo(RoleEnum.SystemAdmin.ToString()));
        }

        [Test]
        public void GenerateToken_ShouldThrow_WhenCancellationRequested()
        {
            var user = new User("test@example.com", "username", "1234567890", "hashed", Guid.NewGuid());
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(() => _service.GenerateToken(user, cts.Token));
        }

        [Test]
        public async Task GenerateToken_ShouldHandleNullPhoneNumberAndNoRoles()
        {
            var user = new User("test2@example.com", "user2", null, "hashed", Guid.NewGuid());
            // No roles added
            var token = await _service.GenerateToken(user, CancellationToken.None);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            // PhoneNumber claim should be absent
            Assert.That(jwt.Claims.FirstOrDefault(c => c.Type == "PhoneNumber"), Is.Null);
            // Roles claim should be absent
            Assert.That(jwt.Claims.Where(c => c.Type == "roles"), Is.Empty);
        }

        // ------------------ CreateAsync Tests ------------------

        [Test]
        public async Task CreateAsync_ShouldReturnTokenService_WhenPrivateKeyFileExists()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                // Generate RSA key and export to PEM
                using var rsa = RSA.Create(2048);
                var privateKey = rsa.ExportRSAPrivateKey();
                var pem = PemEncoding.Write("RSA PRIVATE KEY", privateKey);
                await File.WriteAllTextAsync(tempFile, pem);

                var settings = new JwtSettings
                {
                    PrivateKeyPath = tempFile,
                    Issuer = "Issuer",
                    Audience = "Audience",
                    ExpiryMinutes = 60
                };

                var svc = await TokenService.CreateAsync(settings, CancellationToken.None);

                Assert.That(svc, Is.Not.Null);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void CreateAsync_ShouldThrow_WhenFileNotFound()
        {
            var settings = new JwtSettings { PrivateKeyPath = "nonexistent.pem" };
            Assert.ThrowsAsync<FileNotFoundException>(() => TokenService.CreateAsync(settings, CancellationToken.None));
        }

        [Test]
        public void CreateAsync_ShouldThrow_WhenInvalidPem()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "INVALID PEM");

                var settings = new JwtSettings { PrivateKeyPath = tempFile };

                Assert.ThrowsAsync<ArgumentException>(() => TokenService.CreateAsync(settings, CancellationToken.None));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void CreateAsync_ShouldThrow_WhenCancellationRequested()
        {
            var tempFile = Path.GetTempFileName();
            var cts = new CancellationTokenSource();
            cts.Cancel();
            File.WriteAllText(tempFile, "some key");
            var settings = new JwtSettings { PrivateKeyPath = tempFile };
            Assert.ThrowsAsync<TaskCanceledException>(() => TokenService.CreateAsync(settings, cts.Token));
            File.Delete(tempFile);
        }
    }
}

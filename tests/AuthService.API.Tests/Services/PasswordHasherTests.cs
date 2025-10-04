using AuthService.API.Services;

namespace AuthService.API.Tests.Services
{
    [TestFixture]
    public class PasswordHasherTests
    {
        private PasswordHasher _passwordHasher;

        [SetUp]
        public void Setup()
        {
            _passwordHasher = new PasswordHasher();
        }

        [Test]
        public void HashPassword_ShouldReturnDifferentHashes_ForSamePassword()
        {
            // Arrange
            string password = "MySecret123!";

            // Act
            string hash1 = _passwordHasher.HashPassword(password);
            string hash2 = _passwordHasher.HashPassword(password);

            // Assert
            Assert.That(hash1, Is.Not.Null);
            Assert.That(hash2, Is.Not.Null);
            Assert.That(hash1, Is.Not.Empty);
            Assert.That(hash2, Is.Not.Empty);
            Assert.That(hash1, Is.Not.EqualTo(hash2), "Hash should be unique due to random salt.");
        }

        [Test]
        public void HashPassword_ShouldReturnValidBase64SaltAndHash()
        {
            // Arrange
            string password = "Password123";

            // Act
            string hashed = _passwordHasher.HashPassword(password);
            var parts = hashed.Split(':');

            // Assert
            Assert.That(parts.Length, Is.EqualTo(2), "Hashed password must have 'salt:hash' format.");

            // Both parts should be valid Base64
            Assert.DoesNotThrow(() => Convert.FromBase64String(parts[0]));
            Assert.DoesNotThrow(() => Convert.FromBase64String(parts[1]));
        }

        [Test]
        public void VerifyPassword_ShouldReturnTrue_ForCorrectPassword()
        {
            // Arrange
            string password = "MyPass!";
            string hashed = _passwordHasher.HashPassword(password);

            // Act
            bool result = _passwordHasher.VerifyPassword(password, hashed);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void VerifyPassword_ShouldReturnFalse_ForIncorrectPassword()
        {
            // Arrange
            string password = "MyPass!";
            string wrongPassword = "WrongPass!";
            string hashed = _passwordHasher.HashPassword(password);

            // Act
            bool result = _passwordHasher.VerifyPassword(wrongPassword, hashed);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyPassword_ShouldReturnFalse_ForMalformedHashedPassword()
        {
            // Arrange
            string password = "Anything";
            string invalidHashed = "this-is-not-valid";

            // Act
            bool result = _passwordHasher.VerifyPassword(password, invalidHashed);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyPassword_ShouldReturnFalse_ForEmptyHash()
        {
            // Arrange
            string password = "abc";

            // Act
            bool result = _passwordHasher.VerifyPassword(password, "");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HashPassword_ShouldThrow_ForNullPassword()
        {
            // Arrange, Act & Assert
            Assert.That(() => _passwordHasher.HashPassword(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void VerifyPassword_ShouldHandleEmptyPassword_Gracefully()
        {
            // Arrange
            string hashed = _passwordHasher.HashPassword("password");

            // Act
            bool result = _passwordHasher.VerifyPassword("", hashed);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyPassword_ShouldBeDeterministic_ForSameSalt()
        {
            // Arrange
            string password = "Deterministic!";
            string hashed = _passwordHasher.HashPassword(password);

            // Extract salt
            var parts = hashed.Split(':');
            var salt = parts[0];

            // Create a new hasher and manually recompute with same salt
            var saltBytes = Convert.FromBase64String(salt);
            var reHashed = Convert.ToBase64String(saltBytes) + ":" + Convert.ToBase64String(
                Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivation.Pbkdf2(
                    password,
                    saltBytes,
                    Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivationPrf.HMACSHA256,
                    10000,
                    32));

            // Act
            bool result = _passwordHasher.VerifyPassword(password, reHashed);

            // Assert
            Assert.That(result, Is.True);
        }
    }
}

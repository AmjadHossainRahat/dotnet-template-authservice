using AuthService.Domain.Entities;
using AuthService.Infrastructure.Repositories;
using AuthService.Shared.Services;
using Moq;

namespace AuthService.Infrastructure.Tests.Repositories
{
    [TestFixture]
    public class InMemoryUserRepositoryTests
    {
        private Mock<IPasswordHasher> _passwordHasherMock;
        private InMemoryUserRepository _repository;
        private CancellationToken _token;

        [SetUp]
        public void Setup()
        {
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashedPassword123");
            _passwordHasherMock.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            _repository = new InMemoryUserRepository(_passwordHasherMock.Object);
            _token = CancellationToken.None;
        }

        [Test]
        public async Task AddAsync_ShouldAddUserSuccessfully()
        {
            var tenantId = Guid.NewGuid();
            var user = new User("newuser@example.com", "newuser", "12345", "hashed", tenantId);

            await _repository.AddAsync(user, _token);
            var result = await _repository.GetByIdAsync(user.Id, _token);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Email, Is.EqualTo("newuser@example.com"));
        }

        [Test]
        public async Task UpdateAsync_ShouldUpdateExistingUser()
        {
            var tenantId = Guid.NewGuid();
            var user = new User("user1@example.com", "user1", "1111", "hashed1", tenantId);
            await _repository.AddAsync(user, CancellationToken.None);

            // Update phone number
            var updatedUser = new User(user.Email, user.Username, "0000", user.PasswordHash, tenantId)
            {
                Id = user.Id
            };
            await _repository.UpdateAsync(updatedUser, CancellationToken.None);

            var retrieved = await _repository.GetByIdAsync(user.Id, CancellationToken.None);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.PhoneNumber, Is.EqualTo("0000"));
        }

        [Test]
        public void UpdateAsync_ShouldThrow_WhenUserNotFound()
        {
            var user = new User("nonexist@example.com", "nouser", "1234", "hashed", Guid.NewGuid());
            Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _repository.UpdateAsync(user, CancellationToken.None)
            );
        }

        [Test]
        public async Task DeleteAsync_ShouldRemoveUser()
        {
            var tenantId = Guid.NewGuid();
            var user = new User("delete@example.com", "deluser", "9999", "hashed", tenantId);
            await _repository.AddAsync(user, CancellationToken.None);

            await _repository.DeleteAsync(user.Id, CancellationToken.None);
            var retrieved = await _repository.GetByIdAsync(user.Id, CancellationToken.None);
            Assert.That(retrieved, Is.Null);
        }

        [Test]
        public async Task GetAllAsync_ShouldReturnAllUsers()
        {
            var allUsers = await _repository.GetAllAsync(_token);
            Assert.That(allUsers.Count(), Is.GreaterThanOrEqualTo(4)); // seeded users + any added
        }

        [Test]
        public async Task GetByEmailAsync_ShouldReturnUser_WhenEmailExists()
        {
            var knownUser = (await _repository.GetAllAsync(_token)).First();
            var user = await _repository.GetByEmailAsync(knownUser.Email, _token);

            Assert.That(user, Is.Not.Null);
            Assert.That(user.Email, Is.EqualTo(knownUser.Email));
        }

        [Test]
        public async Task GetByEmailAsync_ShouldReturnNull_WhenEmailNotFound()
        {
            var user = await _repository.GetByEmailAsync("nonexistent@example.com", _token);
            Assert.That(user, Is.Null);
        }

        [Test]
        public async Task GetByUsernameAsync_ShouldReturnUser_WhenExists()
        {
            var knownUser = (await _repository.GetAllAsync(_token)).First();
            var user = await _repository.GetByUsernameAsync(knownUser.Username, _token);

            Assert.That(user, Is.Not.Null);
            Assert.That(user.Username, Is.EqualTo(knownUser.Username));
        }

        [Test]
        public async Task GetByUsernameAsync_ShouldReturnNull_WhenNotExists()
        {
            var user = await _repository.GetByUsernameAsync("unknownuser", _token);
            Assert.That(user, Is.Null);
        }

        [Test]
        public async Task ExistsByEmailAsync_ShouldReturnTrue_WhenEmailExists()
        {
            var knownUser = (await _repository.GetAllAsync(_token)).First();
            var exists = await _repository.ExistsByEmailAsync(knownUser.Email, _token);

            Assert.That(exists, Is.True);
        }

        [Test]
        public async Task ExistsByEmailAsync_ShouldReturnFalse_WhenEmailDoesNotExist()
        {
            var exists = await _repository.ExistsByEmailAsync("fake@domain.com", _token);
            Assert.That(exists, Is.False);
        }

        [Test]
        public async Task ExistsByUsernameAsync_ShouldReturnTrue_WhenUsernameExists()
        {
            var knownUser = (await _repository.GetAllAsync(_token)).First();
            var exists = await _repository.ExistsByUsernameAsync(knownUser.Username, _token);

            Assert.That(exists, Is.True);
        }

        [Test]
        public async Task ExistsByUsernameAsync_ShouldReturnFalse_WhenUsernameDoesNotExist()
        {
            var exists = await _repository.ExistsByUsernameAsync("ghostuser", _token);
            Assert.That(exists, Is.False);
        }

        [Test]
        public async Task GetByIdAsync_ShouldReturnUser_WhenIdExists()
        {
            var knownUser = (await _repository.GetAllAsync(_token)).First();
            var user = await _repository.GetByIdAsync(knownUser.Id, _token);

            Assert.That(user, Is.Not.Null);
            Assert.That(user.Id, Is.EqualTo(knownUser.Id));
        }

        [Test]
        public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
        {
            var user = await _repository.GetByIdAsync(Guid.NewGuid(), _token);
            Assert.That(user, Is.Null);
        }

        [Test]
        public async Task GetByPhoneNumberAsync_ShouldReturnUser_WhenPhoneNumberMatches()
        {
            var tenantId = Guid.NewGuid();
            var user = new User("peter@example.com", "peter", "999999", "hashed", tenantId);
            await _repository.AddAsync(user, _token);

            var result = await _repository.GetByPhoneNumberAsync("999999", _token);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.PhoneNumber, Is.EqualTo("999999"));
        }

        [Test]
        public async Task GetByPhoneNumberAsync_ShouldReturnNull_WhenPhoneNumberDoesNotExist()
        {
            var user = await _repository.GetByPhoneNumberAsync("555555", _token);
            Assert.That(user, Is.Null);
        }

        [Test]
        public async Task GetByLoginIdentifierAsync_ShouldReturnUser_ByEmail()
        {
            var knownUser = (await _repository.GetAllAsync(_token)).First();
            var user = await _repository.GetByLoginIdentifierAsync(knownUser.Email, _token);

            Assert.That(user, Is.Not.Null);
            Assert.That(user.Email, Is.EqualTo(knownUser.Email));
        }

        [Test]
        public async Task GetByLoginIdentifierAsync_ShouldReturnUser_ByUsername()
        {
            var knownUser = (await _repository.GetAllAsync(_token)).First();
            var user = await _repository.GetByLoginIdentifierAsync(knownUser.Username, _token);

            Assert.That(user, Is.Not.Null);
            Assert.That(user.Username, Is.EqualTo(knownUser.Username));
        }

        [Test]
        public async Task GetByLoginIdentifierAsync_ShouldReturnUser_ByPhoneNumber()
        {
            var tenantId = Guid.NewGuid();
            var user = new User("lucy@example.com", "lucy", "7777", "hashed", tenantId);
            await _repository.AddAsync(user, _token);

            var found = await _repository.GetByLoginIdentifierAsync("7777", _token);
            Assert.That(found, Is.Not.Null);
            Assert.That(found.PhoneNumber, Is.EqualTo("7777"));
        }

        [Test]
        public async Task GetByLoginIdentifierAsync_ShouldReturnNull_WhenNoMatch()
        {
            var user = await _repository.GetByLoginIdentifierAsync("nomatch", _token);
            Assert.That(user, Is.Null);
        }
    }
}

using System.Text.Json;
using AuthService.Infrastructure.Caching;
using Moq;
using StackExchange.Redis;

namespace AuthService.Infrastructure.Tests.Caching
{
    [TestFixture]
    public class RedisCacheServiceTests
    {
        private Mock<IConnectionMultiplexer> _mockConnection = null!;
        private Mock<IDatabase> _mockDatabase = null!;
        private RedisCacheService _cacheService = null!;

        [SetUp]
        public void Setup()
        {
            _mockConnection = new Mock<IConnectionMultiplexer>();
            _mockDatabase = new Mock<IDatabase>();
            _mockConnection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                           .Returns(_mockDatabase.Object);

            _cacheService = new RedisCacheService(_mockConnection.Object);
        }

        [TearDown]
        public void Cleanup()
        {
            _cacheService.Dispose();
        }

        // --- SetAsync Tests ---

        [Test]
        public void SetAsync_ShouldThrow_WhenDatabaseThrows()
        {
            // Arrange
            _mockDatabase.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), null, false, When.Always, CommandFlags.None))
                         .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.SocketFailure, "Failed"));

            // Act & Assert
            Assert.That(async () => await _cacheService.SetAsync("fail", "value"), Throws.Exception.TypeOf<RedisConnectionException>());
        }

        // --- GetAsync Tests ---

        [Test]
        public async Task GetAsync_ShouldReturnDeserializedObject_WhenValueExists()
        {
            // Arrange
            var key = "key2";
            var obj = new TestObj { Id = 5, Name = "Bob" };
            var serialized = JsonSerializer.Serialize(obj);

            _mockDatabase.Setup(d => d.StringGetAsync(key, CommandFlags.None))
                         .ReturnsAsync(serialized);

            // Act
            var result = await _cacheService.GetAsync<TestObj>(key);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(5));
            Assert.That(result.Name, Is.EqualTo("Bob"));
        }

        [Test]
        public async Task GetAsync_ShouldReturnDefault_WhenValueIsNullOrEmpty()
        {
            // Arrange
            _mockDatabase.Setup(d => d.StringGetAsync("missing", CommandFlags.None))
                         .ReturnsAsync(RedisValue.Null);

            // Act
            var result = await _cacheService.GetAsync<string>("missing");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetAsync_ShouldThrow_WhenDeserializationFails()
        {
            // Arrange
            _mockDatabase.Setup(d => d.StringGetAsync("bad-json", CommandFlags.None))
                         .ReturnsAsync("not-json");

            // Act & Assert
            Assert.That(async () => await _cacheService.GetAsync<TestObj>("bad-json"),
                Throws.Exception.TypeOf<JsonException>());
        }

        // --- RemoveAsync Tests ---

        [Test]
        public async Task RemoveAsync_ShouldDeleteKey()
        {
            // Arrange
            var key = "key3";
            _mockDatabase.Setup(d => d.KeyDeleteAsync(key, CommandFlags.None))
                         .ReturnsAsync(true);

            // Act
            await _cacheService.RemoveAsync(key);

            // Assert
            _mockDatabase.Verify(d => d.KeyDeleteAsync(key, CommandFlags.None), Times.Once);
        }

        [Test]
        public void RemoveAsync_ShouldThrow_WhenDatabaseThrows()
        {
            // Arrange
            _mockDatabase.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), CommandFlags.None))
                         .ThrowsAsync(new RedisTimeoutException("Timeout", CommandStatus.Unknown));

            // Act & Assert
            Assert.That(async () => await _cacheService.RemoveAsync("any"), Throws.Exception.TypeOf<RedisTimeoutException>());
        }

        // --- ExistsAsync Tests ---

        [Test]
        public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
        {
            // Arrange
            var key = "exists";
            _mockDatabase.Setup(d => d.KeyExistsAsync(key, CommandFlags.None))
                         .ReturnsAsync(true);

            // Act
            var result = await _cacheService.ExistsAsync(key);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
        {
            // Arrange
            var key = "missing";
            _mockDatabase.Setup(d => d.KeyExistsAsync(key, CommandFlags.None))
                         .ReturnsAsync(false);

            // Act
            var result = await _cacheService.ExistsAsync(key);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ExistsAsync_ShouldThrow_WhenDatabaseFails()
        {
            // Arrange
            _mockDatabase.Setup(d => d.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None))
                         .ThrowsAsync(new RedisServerException("Server error"));

            // Act & Assert
            Assert.That(async () => await _cacheService.ExistsAsync("err"), Throws.Exception.TypeOf<RedisServerException>());
        }

        // --- Dispose Tests ---

        [Test]
        public void Dispose_ShouldCallDispose_OnConnection()
        {
            // Arrange
            var mockConn = new Mock<IConnectionMultiplexer>();
            var service = new RedisCacheService(mockConn.Object);

            // Act
            service.Dispose();

            // Assert
            mockConn.Verify(c => c.Dispose(), Times.Once);
        }

        private class TestObj
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }
    }
}

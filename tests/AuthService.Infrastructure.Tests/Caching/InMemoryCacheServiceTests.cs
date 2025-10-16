using AuthService.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace AuthService.Infrastructure.Tests.Caching
{
    [TestFixture]
    public class InMemoryCacheServiceTests
    {
        private IMemoryCache _memoryCache = null!;
        private InMemoryCacheService _cacheService = null!;

        [SetUp]
        public void Setup()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _cacheService = new InMemoryCacheService(_memoryCache);
        }

        [TearDown]
        public void Cleanup()
        {
            _memoryCache.Dispose();
        }

        [Test]
        public async Task SetAsync_ShouldStoreValue_WithoutExpiry()
        {
            // Arrange
            var key = "key1";
            var value = "value1";

            // Act
            await _cacheService.SetAsync(key, value);

            // Assert
            var cachedValue = await _cacheService.GetAsync<string>(key);
            Assert.That(cachedValue, Is.EqualTo(value));
        }

        [Test]
        public async Task SetAsync_ShouldStoreValue_WithExpiry()
        {
            // Arrange
            var key = "key2";
            var value = "value2";
            var expiry = TimeSpan.FromMilliseconds(100);

            // Act
            await _cacheService.SetAsync(key, value, expiry);
            var cachedValueBeforeExpiry = await _cacheService.GetAsync<string>(key);

            // Assert (before expiry)
            Assert.That(cachedValueBeforeExpiry, Is.EqualTo(value));

            // Wait for expiry
            await Task.Delay(200);

            // Act again (after expiry)
            var cachedValueAfterExpiry = await _cacheService.GetAsync<string>(key);

            // Assert
            Assert.That(cachedValueAfterExpiry, Is.Null);
        }

        [Test]
        public async Task GetAsync_ShouldReturnDefault_WhenKeyNotExists()
        {
            // Act
            var result = await _cacheService.GetAsync<string>("missing-key");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetAsync_ShouldReturnCorrectType()
        {
            // Arrange
            var key = "int-key";
            var value = 42;

            await _cacheService.SetAsync(key, value);

            // Act
            var result = await _cacheService.GetAsync<int>(key);

            // Assert
            Assert.That(result, Is.EqualTo(value));
        }

        [Test]
        public async Task RemoveAsync_ShouldRemoveKey()
        {
            // Arrange
            var key = "key3";
            var value = "value3";
            await _cacheService.SetAsync(key, value);

            // Act
            await _cacheService.RemoveAsync(key);
            var cachedValue = await _cacheService.GetAsync<string>(key);

            // Assert
            Assert.That(cachedValue, Is.Null);
        }

        [Test]
        public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
        {
            // Arrange
            var key = "exists-key";
            var value = "present";
            await _cacheService.SetAsync(key, value);

            // Act
            var exists = await _cacheService.ExistsAsync(key);

            // Assert
            Assert.That(exists, Is.True);
        }

        [Test]
        public async Task ExistsAsync_ShouldReturnFalse_WhenKeyMissing()
        {
            // Act
            var exists = await _cacheService.ExistsAsync("no-key");

            // Assert
            Assert.That(exists, Is.False);
        }

        [Test]
        public async Task SetAsync_ShouldOverrideExistingValue()
        {
            // Arrange
            var key = "override-key";
            await _cacheService.SetAsync(key, "old");
            await _cacheService.SetAsync(key, "new");

            // Act
            var result = await _cacheService.GetAsync<string>(key);

            // Assert
            Assert.That(result, Is.EqualTo("new"));
        }
    }
}

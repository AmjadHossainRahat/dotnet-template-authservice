using AuthService.Domain.Entities;
using AuthService.Infrastructure.Repositories;

namespace AuthService.Infrastructure.Tests.Repositories
{
    [TestFixture]
    public class InMemoryTenantRepositoryTests
    {
        private InMemoryTenantRepository _repository;
        private CancellationToken _token;

        [SetUp]
        public void Setup()
        {
            _repository = new InMemoryTenantRepository();
            _token = CancellationToken.None;
        }

        [Test]
        public async Task AddAsync_ShouldAddTenantSuccessfully()
        {
            var tenant = new Tenant("Tenant1");
            await _repository.AddAsync(tenant, _token);

            var retrieved = await _repository.GetByIdAsync(tenant.Id, _token);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Name, Is.EqualTo("Tenant1"));
        }

        [Test]
        public async Task GetAllAsync_ShouldReturnOnlyNonDeletedTenants()
        {
            var tenant1 = new Tenant("T1");
            var tenant2 = new Tenant("T2");
            await _repository.AddAsync(tenant1, _token);
            await _repository.AddAsync(tenant2, _token);

            var allTenants = (await _repository.GetAllAsync(_token)).ToList();
            Assert.That(allTenants.Count, Is.EqualTo(2));

            // Soft delete tenant1
            await _repository.SoftDeleteAsync(tenant1.Id, _token);
            allTenants = (await _repository.GetAllAsync(_token)).ToList();
            Assert.That(allTenants.Count, Is.EqualTo(1));
            Assert.That(allTenants.First().Id, Is.EqualTo(tenant2.Id));
        }

        [Test]
        public async Task GetByIdAsync_ShouldReturnTenant_WhenExists()
        {
            var tenant = new Tenant("TenantX");
            await _repository.AddAsync(tenant, _token);

            var retrieved = await _repository.GetByIdAsync(tenant.Id, _token);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Id, Is.EqualTo(tenant.Id));
        }

        [Test]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDeletedOrNotExist()
        {
            var tenant = new Tenant("TenantY");
            await _repository.AddAsync(tenant, _token);

            await _repository.SoftDeleteAsync(tenant.Id, _token);
            var retrieved = await _repository.GetByIdAsync(tenant.Id, _token);
            Assert.That(retrieved, Is.Null);

            var nonExistent = await _repository.GetByIdAsync(Guid.NewGuid(), _token);
            Assert.That(nonExistent, Is.Null);
        }

        [Test]
        public async Task GetByNameAsync_ShouldReturnTenant_WhenExists()
        {
            var tenant = new Tenant("TenantZ");
            await _repository.AddAsync(tenant, _token);

            var retrieved = await _repository.GetByNameAsync("TenantZ", _token);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Name, Is.EqualTo("TenantZ"));
        }

        [Test]
        public async Task GetByNameAsync_ShouldReturnNull_WhenDeletedOrNotExist()
        {
            var tenant = new Tenant("TenantA");
            await _repository.AddAsync(tenant, _token);

            await _repository.SoftDeleteAsync(tenant.Id, _token);
            var retrieved = await _repository.GetByNameAsync("TenantA", _token);
            Assert.That(retrieved, Is.Null);

            var nonExistent = await _repository.GetByNameAsync("Unknown", _token);
            Assert.That(nonExistent, Is.Null);
        }

        [Test]
        public async Task UpdateAsync_ShouldUpdateTenantSuccessfully()
        {
            var tenant = new Tenant("Original");
            await _repository.AddAsync(tenant, _token);

            tenant.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(tenant, _token);

            var retrieved = await _repository.GetByIdAsync(tenant.Id, _token);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Id, Is.EqualTo(tenant.Id));
        }

        [Test]
        public void UpdateAsync_ShouldThrow_WhenTenantNotFound()
        {
            var tenant = new Tenant("NonExistent");
            Assert.ThrowsAsync<InvalidDataException>(async () =>
                await _repository.UpdateAsync(tenant, _token)
            );
        }

        [Test]
        public async Task DeleteAsync_ShouldRemoveTenant()
        {
            var tenant = new Tenant("ToDelete");
            await _repository.AddAsync(tenant, _token);

            await _repository.DeleteAsync(tenant.Id, _token);

            var retrieved = await _repository.GetByIdAsync(tenant.Id, _token);
            Assert.That(retrieved, Is.Null);
        }

        [Test]
        public void SoftDeleteAsync_ShouldThrow_WhenTenantNotFound()
        {
            Assert.ThrowsAsync<InvalidDataException>(async () =>
                await _repository.SoftDeleteAsync(Guid.NewGuid(), _token)
            );
        }
    }
}

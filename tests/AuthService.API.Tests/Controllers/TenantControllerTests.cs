using AuthService.API.Controllers;
using AuthService.API.Models;
using AuthService.Application.CommandHandlers;
using AuthService.Application.DTOs;
using AuthService.Application.Mediator;
using AuthService.Application.QueryHandlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthService.API.Tests.Controllers
{
    [TestFixture]
    public class TenantControllerTests
    {
        private Mock<IMediator> _mediatorMock;
        private Mock<ILogger<TenantController>> _loggerMock;
        private TenantController _controller;
        private CancellationToken _token;

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<TenantController>>();
            _controller = new TenantController(_mediatorMock.Object, _loggerMock.Object);
            _token = CancellationToken.None;
        }

        #region Create

        [Test]
        public async Task Create_ShouldReturnCreatedAtAction_WhenTenantCreated()
        {
            var dto = new CreateTenantDto { Name = "TenantA" };
            var tenantResult = new TenantDto { Id = Guid.NewGuid(), Name = "TenantA" };

            _mediatorMock.Setup(m => m.SendAsync(It.IsAny<CreateTenantCommand>(), _token))
                         .ReturnsAsync(tenantResult);

            var result = await _controller.Create(dto, _token);
            var objectResult = result.Result as CreatedAtActionResult;

            Assert.That(objectResult, Is.Not.Null);
            var apiResponse = objectResult!.Value as ApiResponse<TenantDto>;
            Assert.That(apiResponse, Is.Not.Null);
            Assert.That(apiResponse!.Data!.Id, Is.EqualTo(tenantResult.Id));
            Assert.That(apiResponse.Data.Name, Is.EqualTo("TenantA"));
        }

        [Test]
        public void Create_ShouldThrow_WhenMediatorThrows()
        {
            var dto = new CreateTenantDto { Name = "TenantA" };
            _mediatorMock.Setup(m => m.SendAsync(It.IsAny<CreateTenantCommand>(), _token))
                         .ThrowsAsync(new Exception("Mediator failed"));

            Assert.ThrowsAsync<Exception>(async () => await _controller.Create(dto, _token));
        }

        #endregion

        #region Update

        [Test]
        public async Task Update_ShouldReturnOk_WhenTenantUpdated()
        {
            var dto = new UpdateTenantDto { Id = Guid.NewGuid(), Name = "UpdatedTenant" };
            var updatedTenant = new TenantDto { Id = dto.Id, Name = dto.Name };

            _mediatorMock.Setup(m => m.SendAsync(It.IsAny<UpdateTenantCommand>(), _token))
                         .ReturnsAsync(updatedTenant);

            var result = await _controller.Update(dto, _token);
            var objectResult = result.Result as OkObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            var apiResponse = objectResult!.Value as ApiResponse<TenantDto>;
            Assert.That(apiResponse, Is.Not.Null);
            Assert.That(apiResponse!.Data!.Name, Is.EqualTo("UpdatedTenant"));
        }

        [Test]
        public void Update_ShouldThrow_WhenMediatorThrows()
        {
            var dto = new UpdateTenantDto { Id = Guid.NewGuid(), Name = "UpdatedTenant" };
            _mediatorMock.Setup(m => m.SendAsync(It.IsAny<UpdateTenantCommand>(), _token))
                         .ThrowsAsync(new Exception("Update failed"));

            Assert.ThrowsAsync<Exception>(async () => await _controller.Update(dto, _token));
        }

        #endregion

        #region Delete

        [Test]
        public void Delete_ShouldThrow_WhenMediatorThrows()
        {
            var tenantId = Guid.NewGuid();
            _mediatorMock.Setup(m => m.SendAsync(It.IsAny<UpdateTenantCommand>(), _token))
                         .ThrowsAsync(new Exception("Delete failed"));

            Assert.ThrowsAsync<Exception>(async () => await _controller.Delete(tenantId, _token));
        }

        #endregion

        #region GetById

        [Test]
        public async Task GetById_ShouldReturnOk_WhenTenantExists()
        {
            var tenantId = Guid.NewGuid();
            var tenantDto = new TenantDto { Id = tenantId, Name = "TenantX" };

            _mediatorMock.Setup(m => m.SendAsync(It.Is<GetTenantByIdQuery>(q => q.Id == tenantId), _token))
                         .ReturnsAsync(tenantDto);

            var result = await _controller.GetById(tenantId, _token);
            var objectResult = result.Result as OkObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            var apiResponse = objectResult!.Value as ApiResponse<TenantDto>;
            Assert.That(apiResponse, Is.Not.Null);
            Assert.That(apiResponse!.Data!.Id, Is.EqualTo(tenantId));
        }

        #endregion

        #region GetAll

        [Test]
        public async Task GetAll_ShouldReturnOk_WithListOfTenants()
        {
            var tenants = new List<TenantDto>
            {
                new TenantDto { Id = Guid.NewGuid(), Name = "Tenant1" },
                new TenantDto { Id = Guid.NewGuid(), Name = "Tenant2" }
            };

            _mediatorMock.Setup(m => m.SendAsync(It.IsAny<GetAllTenantsQuery>(), _token))
                         .ReturnsAsync(tenants);

            var result = await _controller.GetAll(_token);
            var objectResult = result.Result as OkObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            var apiResponse = objectResult!.Value as ApiResponse<IEnumerable<TenantDto>>;
            Assert.That(apiResponse, Is.Not.Null);
            Assert.That(apiResponse!.Data!.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetAll_ShouldReturnOk_WhenNoTenants()
        {
            _mediatorMock.Setup(m => m.SendAsync(It.IsAny<GetAllTenantsQuery>(), _token))
                         .ReturnsAsync(new List<TenantDto>());

            var result = await _controller.GetAll(_token);
            var objectResult = result.Result as OkObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            var apiResponse = objectResult!.Value as ApiResponse<IEnumerable<TenantDto>>;
            Assert.That(apiResponse, Is.Not.Null);
            Assert.That(apiResponse!.Data!.Count(), Is.EqualTo(0));
        }

        [Test]
        public void GetAll_ShouldThrow_WhenMediatorThrows()
        {
            _mediatorMock.Setup(m => m.SendAsync(It.IsAny<GetAllTenantsQuery>(), _token))
                         .ThrowsAsync(new Exception("Mediator error"));

            Assert.ThrowsAsync<Exception>(async () => await _controller.GetAll(_token));
        }

        #endregion
    }
}

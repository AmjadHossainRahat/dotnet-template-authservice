using AuthService.API.Controllers;
using AuthService.API.Models;
using AuthService.API.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using AuthService.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.API.Tests.Controllers
{
    [TestFixture]
    public class AuthControllerTests
    {
        private Mock<ITokenService> _tokenServiceMock;
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<IPasswordHasher> _passwordHasherMock;
        private AuthController _controller;

        [SetUp]
        public void Setup()
        {
            _tokenServiceMock = new Mock<ITokenService>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();

            _controller = new AuthController(_tokenServiceMock.Object, _userRepositoryMock.Object, _passwordHasherMock.Object);
        }

        #region Register Tests

        [Test]
        public async Task Register_ShouldReturnConflict_WhenEmailExists()
        {
            _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var request = new RegisterRequest { Email = "test@test.com", Username = "user1", Password = "pass", TenantId = Guid.NewGuid().ToString() };
            var result = await _controller.Register(request, CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(409));
        }

        [Test]
        public async Task Register_ShouldReturnConflict_WhenUsernameExists()
        {
            _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _userRepositoryMock.Setup(r => r.ExistsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var request = new RegisterRequest { Email = "test@test.com", Username = "user1", Password = "pass", TenantId = Guid.NewGuid().ToString() };
            var result = await _controller.Register(request, CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(409));
        }

        [Test]
        public async Task Register_ShouldReturnOk_WhenUserCreated()
        {
            _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _userRepositoryMock.Setup(r => r.ExistsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashedPassword");

            var tenantId = Guid.NewGuid();
            var request = new RegisterRequest { Email = "test@test.com", Username = "user1", Password = "pass", TenantId = tenantId.ToString() };

            var result = await _controller.Register(request, CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));
        }

        #endregion

        #region Login Tests

        [Test]
        public async Task Login_ShouldReturnUnauthorized_WhenUserNotFound()
        {
            _userRepositoryMock.Setup(r => r.GetByLoginIdentifierAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User)null);

            var request = new LoginRequest { LoginIdentifier = "user1", Password = "pass" };
            var result = await _controller.Login(request, CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task Login_ShouldReturnUnauthorized_WhenPasswordInvalid()
        {
            var user = new User("test@test.com", "user1", "1234567890", "hashedPassword", Guid.NewGuid());
            _userRepositoryMock.Setup(r => r.GetByLoginIdentifierAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _passwordHasherMock.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            var request = new LoginRequest { LoginIdentifier = "user1", Password = "wrongPass" };
            var result = await _controller.Login(request, CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task Login_ShouldReturnOk_WhenCredentialsValid()
        {
            var user = new User("test@test.com", "user1", "1234567890", "hashedPassword", Guid.NewGuid());
            user.AssignRole(new Role(RoleEnum.TenantAdmin, user.TenantId));

            _userRepositoryMock.Setup(r => r.GetByLoginIdentifierAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _passwordHasherMock.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _tokenServiceMock.Setup(t => t.GenerateToken(user, It.IsAny<CancellationToken>())).ReturnsAsync("accessToken");

            var request = new LoginRequest { LoginIdentifier = "user1", Password = "pass" };
            var result = await _controller.Login(request, CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));
        }

        #endregion

        #region RefreshToken Tests

        [Test]
        public async Task RefreshToken_ShouldReturnUnauthorized_WhenTokenNotFound()
        {
            var request = new RefreshTokenRequest { RefreshToken = "invalidToken" };

            var result = await _controller.RefreshToken(request, CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task RefreshToken_ShouldReturnUnauthorized_WhenTokenExpired()
        {
            var expiredToken = "expiredToken";
            var userId = Guid.NewGuid();
            var expiry = DateTime.UtcNow.AddMinutes(-1);

            // Manually add token to in-memory dictionary
            var refreshTokensField = typeof(AuthController).GetField("_refreshTokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var dictionary = (Dictionary<string, (Guid UserId, DateTime Expiry)>)refreshTokensField!.GetValue(null)!;
            dictionary[expiredToken] = (userId, expiry);

            var request = new RefreshTokenRequest { RefreshToken = expiredToken };
            var result = await _controller.RefreshToken(request, CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task RefreshToken_ShouldReturnUnauthorized_WhenUserNotFound()
        {
            var token = "token1";
            var userId = Guid.NewGuid();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            var refreshTokensField = typeof(AuthController).GetField("_refreshTokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var dictionary = (Dictionary<string, (Guid UserId, DateTime Expiry)>)refreshTokensField!.GetValue(null)!;
            dictionary[token] = (userId, expiry);

            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null);

            var request = new RefreshTokenRequest { RefreshToken = token };
            var result = await _controller.RefreshToken(request, CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task RefreshToken_ShouldReturnOk_WhenValid()
        {
            var token = "validToken";
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var expiry = DateTime.UtcNow.AddMinutes(10);
            var user = new User("test@example.com", "user1", "1234567890", "hashedPassword", tenantId);
            user.AssignRole(new Role(RoleEnum.TenantAdmin, tenantId));

            var refreshTokensField = typeof(AuthController).GetField("_refreshTokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var dictionary = (Dictionary<string, (Guid UserId, DateTime Expiry)>)refreshTokensField!.GetValue(null)!;
            dictionary[token] = (userId, expiry);

            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _tokenServiceMock.Setup(t => t.GenerateToken(user, It.IsAny<CancellationToken>())).ReturnsAsync("newAccessToken");

            var request = new RefreshTokenRequest { RefreshToken = token };
            var result = await _controller.RefreshToken(request, CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));

            var response = objectResult.Value as ApiResponse<LoginResponse>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.Data.AccessToken, Is.EqualTo("newAccessToken"));
        }

        #endregion

        #region Logout Tests

        [Test]
        public async Task Logout_ShouldRemoveRefreshToken_WhenExists()
        {
            var token = "refreshTokenLogout";
            var userId = Guid.NewGuid();

            var refreshTokensField = typeof(AuthController).GetField("_refreshTokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var dictionary = (Dictionary<string, (Guid UserId, DateTime Expiry)>)refreshTokensField!.GetValue(null)!;
            dictionary[token] = (userId, DateTime.UtcNow.AddMinutes(10));

            var request = new LogoutRequest { RefreshToken = token };
            var result = await _controller.Logout(request, CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));
            Assert.That(dictionary.ContainsKey(token), Is.False);
        }

        [Test]
        public async Task Logout_ShouldReturnOk_WhenTokenDoesNotExist()
        {
            var request = new LogoutRequest { RefreshToken = "nonexistentToken" };
            var result = await _controller.Logout(request, CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));
        }

        #endregion

        #region Me Tests

        [Test]
        public async Task Me_ShouldReturnUnauthorized_WhenUserIdClaimInvalid()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };

            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "invalid-guid") }, "Test")
            );

            var result = await _controller.Me(CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task Me_ShouldReturnUnauthorized_WhenUserNotFound()
        {
            var userId = Guid.NewGuid();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };

            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "Test")
            );

            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((User)null);

            var result = await _controller.Me(CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task Me_ShouldReturnOk_WhenUserExists()
        {
            var tenantId = Guid.NewGuid();
            var user = new User("test@example.com", "user1", "1234567890", "hashedPassword", tenantId);
            user.AssignRole(new Role(RoleEnum.TenantAdmin, tenantId));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };

            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }, "Test")
            );

            _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

            var result = await _controller.Me(CancellationToken.None);
            var objectResult = result.Result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));

            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.Data, Is.Not.Null);
        }

        #endregion
    }
}

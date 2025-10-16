using AuthService.API.Controllers;
using AuthService.API.Models;
using AuthService.API.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Caching;
using AuthService.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AuthService.API.Tests.Controllers
{
    [TestFixture]
    public class AuthControllerTests
    {
        private Mock<ITokenService> _tokenServiceMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<IPasswordHasher> _hasherMock = null!;
        private Mock<ICacheService> _cacheMock = null!;
        private AuthController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _tokenServiceMock = new Mock<ITokenService>();
            _userRepoMock = new Mock<IUserRepository>();
            _hasherMock = new Mock<IPasswordHasher>();
            _cacheMock = new Mock<ICacheService>();

            _controller = new AuthController(
                _tokenServiceMock.Object,
                _userRepoMock.Object,
                _hasherMock.Object,
                _cacheMock.Object
            );
        }

        // ------------------ Register Tests ------------------

        [Test]
        public async Task Register_ShouldReturnConflict_WhenEmailExists()
        {
            var req = new RegisterRequest { Email = "e@x.com", Username = "user", TenantId = Guid.NewGuid().ToString(), Password = "pass" };
            _userRepoMock.Setup(x => x.ExistsByEmailAsync(req.Email, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _controller.Register(req, CancellationToken.None);
            var conflict = result.Result as ConflictObjectResult;

            Assert.That(conflict, Is.Not.Null);
            Assert.That(conflict.StatusCode, Is.EqualTo(409));
        }

        [Test]
        public async Task Register_ShouldReturnConflict_WhenUsernameExists()
        {
            var req = new RegisterRequest { Email = "e@x.com", Username = "user", TenantId = Guid.NewGuid().ToString(), Password = "pass" };
            _userRepoMock.Setup(x => x.ExistsByEmailAsync(req.Email, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _userRepoMock.Setup(x => x.ExistsByUsernameAsync(req.Username, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _controller.Register(req, CancellationToken.None);
            var conflict = result.Result as ConflictObjectResult;

            Assert.That(conflict, Is.Not.Null);
            Assert.That(conflict.StatusCode, Is.EqualTo(409));
        }

        [Test]
        public void Register_ShouldThrow_WhenTenantIdMissing()
        {
            var req = new RegisterRequest { Email = "e@x.com", Username = "user", Password = "pass" };
            _userRepoMock.Setup(x => x.ExistsByEmailAsync(req.Email, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _userRepoMock.Setup(x => x.ExistsByUsernameAsync(req.Username, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            Assert.ThrowsAsync<MissingFieldException>(() => _controller.Register(req, CancellationToken.None));
        }

        [Test]
        public async Task Register_ShouldReturnOk_WhenValid()
        {
            var req = new RegisterRequest { Email = "e@x.com", Username = "user", TenantId = Guid.NewGuid().ToString(), Password = "pass" };
            _userRepoMock.Setup(x => x.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _userRepoMock.Setup(x => x.ExistsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _hasherMock.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed");
            _userRepoMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _controller.Register(req, CancellationToken.None);
            var ok = result.Result as OkObjectResult;

            Assert.That(ok, Is.Not.Null);
            Assert.That(ok.StatusCode, Is.EqualTo(200));
        }

        // ------------------ Login Tests ------------------

        [Test]
        public async Task Login_ShouldReturnCachedResponse_WhenCacheHit()
        {
            var req = new LoginRequest { LoginIdentifier = "user", Password = "pass" };
            var cachedResponse = new LoginResponse { AccessToken = "token" };

            _cacheMock.Setup(x => x.GetAsync<string>($"users_id:{req.LoginIdentifier}"))
                .ReturnsAsync("uid");
            _cacheMock.Setup(x => x.GetAsync<LoginResponse>("user_tokens:uid"))
                .ReturnsAsync(cachedResponse);

            var result = await _controller.Login(req, CancellationToken.None);
            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        [Test]
        public async Task Login_ShouldReturnUnauthorized_WhenInvalidCredentials()
        {
            var req = new LoginRequest { LoginIdentifier = "user", Password = "wrong" };
            _cacheMock.Setup(x => x.GetAsync<string>($"users_id:{req.LoginIdentifier}"))
                .ReturnsAsync((string?)null);
            _userRepoMock.Setup(x => x.GetByLoginIdentifierAsync(req.LoginIdentifier, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var result = await _controller.Login(req, CancellationToken.None);
            var unauthorized = result.Result as UnauthorizedObjectResult;
            Assert.That(unauthorized, Is.Not.Null);
        }

        [Test]
        public async Task Login_ShouldReturnOk_WhenValid()
        {
            var req = new LoginRequest { LoginIdentifier = "user", Password = "pass" };
            var user = new User("e", "u", "123", "hashed", Guid.NewGuid());
            _cacheMock.Setup(x => x.GetAsync<string>($"users_id:{req.LoginIdentifier}"))
                .ReturnsAsync((string?)null);
            _userRepoMock.Setup(x => x.GetByLoginIdentifierAsync(req.LoginIdentifier, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _hasherMock.Setup(x => x.VerifyPassword(req.Password, user.PasswordHash)).Returns(true);
            _tokenServiceMock.Setup(x => x.GenerateToken(user, It.IsAny<CancellationToken>())).ReturnsAsync("token");

            var result = await _controller.Login(req, CancellationToken.None);
            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            _cacheMock.Verify(x => x.SetAsync<LoginResponse>(It.IsAny<string>(), It.IsAny<LoginResponse>(), It.IsAny<TimeSpan?>()), Times.AtLeastOnce);
        }

        // ------------------ RefreshToken Tests ------------------

        [Test]
        public async Task RefreshToken_ShouldReturnOk_WhenCachedValid()
        {
            var req = new RefreshTokenRequest { RefreshToken = "ref" };
            var cached = new LoginResponse { RefreshToken = "ref", RefreshTokenExpiry = DateTime.UtcNow.AddDays(1) };
            _cacheMock.Setup(x => x.GetAsync<LoginResponse>("refresh_token:ref")).ReturnsAsync(cached);

            var result = await _controller.RefreshToken(req, CancellationToken.None);
            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        [Test]
        public async Task RefreshToken_ShouldReturnUnauthorized_WhenCacheMissing()
        {
            var req = new RefreshTokenRequest { RefreshToken = "ref" };
            _cacheMock.Setup(x => x.GetAsync<LoginResponse>("refresh_token:ref")).ReturnsAsync((LoginResponse?)null);

            var result = await _controller.RefreshToken(req, CancellationToken.None);
            var unauthorized = result.Result as UnauthorizedObjectResult;
            Assert.That(unauthorized, Is.Not.Null);
        }

        [Test]
        public async Task RefreshToken_ShouldRegenerateTokens_WhenCacheExpired()
        {
            var req = new RefreshTokenRequest { RefreshToken = "ref" };
            var expired = new LoginResponse { RefreshToken = "ref", RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1) };
            _cacheMock.Setup(x => x.GetAsync<LoginResponse>("refresh_token:ref")).ReturnsAsync(expired);

            var userId = Guid.NewGuid();
            _controller.ControllerContext = new ControllerContext();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                }))
            };

            var user = new User("e", "u", "p", "h", Guid.NewGuid());
            _userRepoMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _tokenServiceMock.Setup(x => x.GenerateToken(user, It.IsAny<CancellationToken>())).ReturnsAsync("token");

            var result = await _controller.RefreshToken(req, CancellationToken.None);
            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        // ------------------ Logout Tests ------------------

        [Test]
        public async Task Logout_ShouldReturnUnauthorized_WhenInvalidClaim()
        {
            var req = new LogoutRequest { RefreshToken = "ref" };
            _controller.ControllerContext = new ControllerContext();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[0]))
            };

            var result = await _controller.Logout(req, CancellationToken.None);
            var unauthorized = result.Result as UnauthorizedObjectResult;
            Assert.That(unauthorized, Is.Not.Null);
        }

        [Test]
        public async Task Logout_ShouldReturnUnauthorized_WhenCacheMissing()
        {
            var req = new LogoutRequest { RefreshToken = "ref" };
            var uid = Guid.NewGuid();
            _controller.ControllerContext = new ControllerContext();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, uid.ToString()) }))
            };
            _cacheMock.Setup(x => x.GetAsync<LoginResponse>($"refresh_token:{req.RefreshToken}")).ReturnsAsync((LoginResponse?)null);

            var result = await _controller.Logout(req, CancellationToken.None);
            var unauthorized = result.Result as UnauthorizedObjectResult;
            Assert.That(unauthorized, Is.Not.Null);
        }

        [Test]
        public async Task Logout_ShouldReturnOk_WhenValid()
        {
            var req = new LogoutRequest { RefreshToken = "ref" };
            var uid = Guid.NewGuid();
            var cached = new LoginResponse { RefreshToken = "ref" };
            _controller.ControllerContext = new ControllerContext();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, uid.ToString()) }))
            };
            _cacheMock.Setup(x => x.GetAsync<LoginResponse>($"refresh_token:{req.RefreshToken}")).ReturnsAsync(cached);

            var result = await _controller.Logout(req, CancellationToken.None);
            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        // ------------------ Me Tests ------------------

        [Test]
        public async Task Me_ShouldReturnUnauthorized_WhenInvalidClaim()
        {
            _controller.ControllerContext = new ControllerContext();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[0]))
            };

            var result = await _controller.Me(CancellationToken.None);
            var unauthorized = result.Result as UnauthorizedObjectResult;
            Assert.That(unauthorized, Is.Not.Null);
        }

        [Test]
        public async Task Me_ShouldReturnCached_WhenCacheHit()
        {
            var uid = Guid.NewGuid();
            _controller.ControllerContext = new ControllerContext();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, uid.ToString()) }))
            };
            _cacheMock.Setup(x => x.GetAsync<object>($"me:{uid}")).ReturnsAsync(new { Name = "cached" });

            var result = await _controller.Me(CancellationToken.None);
            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }

        [Test]
        public async Task Me_ShouldReturnUnauthorized_WhenUserNotFound()
        {
            var uid = Guid.NewGuid();
            _controller.ControllerContext = new ControllerContext();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, uid.ToString()) }))
            };
            _cacheMock.Setup(x => x.GetAsync<object>($"me:{uid}")).ReturnsAsync((object?)null);
            _userRepoMock.Setup(x => x.GetByIdAsync(uid, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

            var result = await _controller.Me(CancellationToken.None);
            var unauthorized = result.Result as UnauthorizedObjectResult;
            Assert.That(unauthorized, Is.Not.Null);
        }

        [Test]
        public async Task Me_ShouldReturnOk_WhenValid()
        {
            var uid = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var user = new User("e", "u", "p", "h", Guid.NewGuid());
            user.Roles.Add(new Role(RoleEnum.SystemAdmin, tenantId));

            _controller.ControllerContext = new ControllerContext();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, uid.ToString()) }))
            };

            _cacheMock.Setup(x => x.GetAsync<object>($"me:{uid}")).ReturnsAsync((object?)null);
            _userRepoMock.Setup(x => x.GetByIdAsync(uid, It.IsAny<CancellationToken>())).ReturnsAsync(user);

            var result = await _controller.Me(CancellationToken.None);
            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
        }
    }
}
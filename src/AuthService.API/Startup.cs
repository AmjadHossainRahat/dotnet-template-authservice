using AuthService.API.Authorization;
using AuthService.API.Extensions;
using AuthService.API.Services;
using AuthService.API.Settings;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Repositories;
using AuthService.Shared.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;

namespace AuthService.API
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Core services
            services.AddCustomMediator();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<ITenantRepository, InMemoryTenantRepository>();
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddHttpContextAccessor();

            // Endpoint roles
            var endpointRolesSettings = new EndpointRolesSettings();
            _configuration.GetSection("EndpointRoles").Bind(endpointRolesSettings.RolesPerEndpoint);
            services.AddSingleton(endpointRolesSettings);

            services.AddAuthorization(options =>
            {
                options.AddPolicy("EndpointRolesPolicy", policy =>
                    policy.Requirements.Add(new EndpointRolesRequirement()));
            });
            services.AddSingleton<IAuthorizationHandler, EndpointRolesRequirementHandler>();

            // JWT Settings & TokenService
            var jwtSettings = ConfigureJwtSettings();
            services.AddSingleton(jwtSettings);

            var tokenService = TokenService.CreateAsync(jwtSettings, CancellationToken.None).GetAwaiter().GetResult();
            services.AddSingleton<ITokenService>(tokenService);

            // RSA for JWT validation
            var rsa = RSA.Create();
            var publicKey = File.ReadAllText(jwtSettings.PublicKeyPath);
            rsa.ImportFromPem(publicKey);
            services.AddSingleton(rsa);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                        AuthService.API.Extensions.JwtBearerExtensions.ConfigureJwtBearer(options, jwtSettings, rsa));

            services.AddControllers();
            services.AddSwaggerGen(SwaggerExtensions.ConfigureSwaggerGen);
        }

        public void Configure(WebApplication app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<Middleware.GlobalExceptionMiddleware>();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
        }

        private JwtSettings ConfigureJwtSettings()
        {
            var section = _configuration.GetSection("JwtSettings");
            if (!section.Exists())
                throw new InvalidOperationException("JwtSettings section is missing.");

            var settings = section.Get<JwtSettings>() ?? throw new InvalidOperationException("Failed to bind JwtSettings.");

            settings.PrivateKeyPath = Path.Combine(_environment.ContentRootPath, "..", "..", "Keys", "private.key");
            settings.PublicKeyPath = Path.Combine(_environment.ContentRootPath, "..", "..", "Keys", "public.key");

            return settings;
        }
    }
}

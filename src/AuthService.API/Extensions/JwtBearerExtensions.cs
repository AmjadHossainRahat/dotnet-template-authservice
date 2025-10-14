using AuthService.API.Services;
using AuthService.API.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AuthService.API.Extensions
{
    public static class JwtBearerExtensions
    {
        public static IServiceCollection SetupJwtBearer(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // JWT Settings & TokenService
            var jwtSettings = ConfigureJwtSettings(configuration, environment);
            services.AddSingleton(jwtSettings);

            var tokenService = TokenService.CreateAsync(jwtSettings, CancellationToken.None).GetAwaiter().GetResult();
            services.AddSingleton<ITokenService>(tokenService);

            // RSA for JWT validation
            var rsa = RSA.Create();
            var publicKey = File.ReadAllText(jwtSettings.PublicKeyPath);
            rsa.ImportFromPem(publicKey);
            services.AddSingleton(rsa);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options => ConfigureJwtBearer(options, jwtSettings, rsa));

            return services;
        }
        private static void ConfigureJwtBearer(JwtBearerOptions options, JwtSettings jwtSettings, RSA rsa)
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new RsaSecurityKey(rsa),
                NameClaimType = JwtRegisteredClaimNames.Sub,
                RoleClaimType = ClaimTypes.Role
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    Console.WriteLine("JWT Authentication failed: " + ctx.Exception);
                    return Task.CompletedTask;
                },
                OnTokenValidated = ctx =>
                {
                    Console.WriteLine("JWT validated successfully for user: " + ctx.Principal?.Identity?.Name);
                    return Task.CompletedTask;
                },
                OnChallenge = ctx =>
                {
                    Console.WriteLine("JWT Challenge: " + ctx.ErrorDescription);
                    return Task.CompletedTask;
                }
            };
        }

        private static JwtSettings ConfigureJwtSettings(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var section = configuration.GetSection("JwtSettings");
            if (!section.Exists())
                throw new InvalidOperationException("JwtSettings section is missing.");

            var settings = section.Get<JwtSettings>() ?? throw new InvalidOperationException("Failed to bind JwtSettings.");

            settings.PrivateKeyPath = Path.Combine(environment.ContentRootPath, "..", "..", "Keys", "private.key");
            settings.PublicKeyPath = Path.Combine(environment.ContentRootPath, "..", "..", "Keys", "public.key");

            return settings;
        }
    }
}

using AuthService.API.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AuthService.API.Extensions
{
    public static class JwtBearerExtensions
    {
        public static void ConfigureJwtBearer(JwtBearerOptions options, JwtSettings jwtSettings, RSA rsa)
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
    }
}

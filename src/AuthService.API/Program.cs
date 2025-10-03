using AuthService.API.Authorization;
using AuthService.API.Services;
using AuthService.API.Settings;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Repositories;
using AuthService.Shared.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddCustomMediator();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<ITenantRepository, InMemoryTenantRepository>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

builder.Services.AddHttpContextAccessor();

var endpointRolesSettings = new EndpointRolesSettings();
builder.Configuration.GetSection("EndpointRoles").Bind(endpointRolesSettings.RolesPerEndpoint);
builder.Services.AddSingleton<EndpointRolesSettings>(endpointRolesSettings);


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EndpointRolesPolicy", policy =>
        policy.Requirements.Add(new EndpointRolesRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, EndpointRolesRequirementHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Template AuthService API", Version = "v1" });

    // Add JWT Bearer Authorization
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        // Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJSUzI1NiIsInR5cCI...\""
        Description = "Enter your valid token in the text input below.\r\n\r\nExample: \"eyJhbGciOiJSUzI1NiIsInR5cCI...\""
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
if (!jwtSettingsSection.Exists())
    throw new InvalidOperationException("JwtSettings section is missing in appsettings.json.");

var jwtSettings = jwtSettingsSection.Get<JwtSettings>();
if (jwtSettings is null)
    throw new InvalidOperationException("Failed to bind JwtSettings from configuration.");

jwtSettings.PrivateKeyPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "Keys", "private.key"));
jwtSettings.PublicKeyPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "Keys", "public.key"));

builder.Services.AddSingleton<JwtSettings>(jwtSettings);

var tokenService = await TokenService.CreateAsync(jwtSettings, CancellationToken.None);
builder.Services.AddSingleton<ITokenService>(tokenService);

var rsa = RSA.Create();
var publicKey = File.ReadAllText(jwtSettings.PublicKeyPath);
rsa.ImportFromPem(publicKey);
builder.Services.AddSingleton(rsa); // optional, reuse RSA instance

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
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

        // Map JWT claims correctly
        NameClaimType = JwtRegisteredClaimNames.Sub, // maps to User.Identity.Name
        RoleClaimType = ClaimTypes.Role               // maps to User.IsInRole(...)
    };

    // Enable events for detailed debugging : NOTE: Comment the code in Production!!!!
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // Log the exception
            Console.WriteLine("JWT Authentication failed: " + context.Exception);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("JWT validated successfully for user: " + context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine("JWT Challenge: " + context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();


if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseMiddleware<AuthService.API.Middleware.GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

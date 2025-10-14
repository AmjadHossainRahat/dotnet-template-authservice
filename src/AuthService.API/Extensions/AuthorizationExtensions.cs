using AuthService.API.Authorization;
using AuthService.API.Settings;
using Microsoft.AspNetCore.Authorization;

namespace AuthService.API.Extensions
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddCustomAuthorization(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind EndpointRolesSettings from appsettings.json
            var endpointRolesSettings = new EndpointRolesSettings();
            configuration.GetSection("EndpointRoles").Bind(endpointRolesSettings.RolesPerEndpoint);
            services.AddSingleton(endpointRolesSettings);

            // Register the custom policy and requirement
            services.AddAuthorization(options =>
            {
                options.AddPolicy("EndpointRolesPolicy", policy =>
                    policy.Requirements.Add(new EndpointRolesRequirement()));
            });

            // Add the custom authorization handler
            services.AddSingleton<IAuthorizationHandler, EndpointRolesRequirementHandler>();

            return services;
        }
    }
}

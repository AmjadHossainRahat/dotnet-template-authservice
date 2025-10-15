using AuthService.API.Extensions;
using AuthService.API.Middleware;
using AuthService.API.Services;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Repositories;
using AuthService.Shared.Services;

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
            // Add EF DbContext
            services.AddApplicationDatabase(_configuration, _environment);

            if (_environment.IsDevelopment())
            {
                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy
                            .AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
                });
            }

            // Core services
            services.AddCustomMediator();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            //services.AddSingleton<ITenantRepository, InMemoryTenantRepository>();
            services.AddScoped<ITenantRepository, EfTenantRepository>();
            //services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddScoped<IUserRepository, EfUserRepository>();

            services.AddHttpContextAccessor();

            services.AddCustomAuthorization(_configuration);
            services.SetupJwtBearer(_configuration, _environment);

            services.AddCustomHealthChecks();

            services.AddControllers();
            services.AddSwaggerGen(SwaggerExtensions.ConfigureSwaggerGen);
        }

        public void Configure(WebApplication app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseApplicationDatabase(env);    // migrate automatically in Dev only

                app.UseRouting();
                app.UseCors();

                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<CustomGlobalExceptionMiddleware>();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCustomHealthChecks();

            app.MapControllers();
        }
    }
}

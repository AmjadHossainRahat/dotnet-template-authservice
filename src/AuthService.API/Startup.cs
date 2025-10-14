using AuthService.API.Extensions;
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
            // Core services
            services.AddCustomMediator();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<ITenantRepository, InMemoryTenantRepository>();
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddHttpContextAccessor();

            services.AddCustomAuthorization(_configuration);
            services.SetupJwtBearer(_configuration, _environment);

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
    }
}

using AuthService.Infrastructure.Caching;
using StackExchange.Redis;

namespace AuthService.API.Extensions
{
    public static class CachingExtension
    {
        public static IServiceCollection AddCustomCaching(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                services.AddMemoryCache();
                services.AddSingleton<ICacheService, InMemoryCacheService>();
            }
            else
            {
                var redisConnection = configuration.GetConnectionString("Redis");
                if (string.IsNullOrEmpty(redisConnection))
                    throw new InvalidOperationException("Redis connection string missing in appsettings.json.");

                services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));
                services.AddSingleton<ICacheService, RedisCacheService>();
            }

            return services;
        }
    }
}

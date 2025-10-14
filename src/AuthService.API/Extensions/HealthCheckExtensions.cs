using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace AuthService.API.Extensions
{
    public static class HealthCheckExtensions
    {
        private static short _memoryThresholdValueinMB = 512;
        public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy("AuthService API is running"))
                .AddCheck("memory", () =>
                {
                    var allocated = GC.GetTotalMemory(false);
                    return allocated < _memoryThresholdValueinMB * 1024 * 1024 // 512 MB threshold
                        ? HealthCheckResult.Healthy($"Memory OK: {allocated / 1024 / 1024} MB")
                        : HealthCheckResult.Degraded($"High memory usage: {allocated / 1024 / 1024} MB");
                });

            return services;
        }

        public static IApplicationBuilder UseCustomHealthChecks(this IApplicationBuilder app)
        {
            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString(),
                        results = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            description = e.Value.Description
                        })
                    });
                    await context.Response.WriteAsync(result);
                }
            });

            return app;
        }
    }
}

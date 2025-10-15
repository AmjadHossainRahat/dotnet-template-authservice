using AuthService.API.Seed;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace AuthService.API.Extensions
{
    public static class DatabaseExtensions
    {
        public static IServiceCollection AddApplicationDatabase(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            if (env.IsDevelopment() || env.EnvironmentName == "Local")
            {
                var sqliteConnection = configuration.GetConnectionString("SQLite");
                if (string.IsNullOrEmpty(sqliteConnection))
                    throw new InvalidOperationException("SQLite connection string is missing in appsettings.json.");

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(sqliteConnection));
            }
            else
            {
                var postgresConnection = configuration.GetConnectionString("Postgres");
                if (string.IsNullOrEmpty(postgresConnection))
                    throw new InvalidOperationException("Postgres connection string is missing in appsettings.json.");

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(postgresConnection));
            }

            return services;
        }

        public static IApplicationBuilder UseApplicationDatabase(this IApplicationBuilder app, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                // Automatically create or update SQLite database
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    //db.Database.Migrate(); // Creates database & applies migrations
                    db.Database.EnsureCreated();    // creates tables based on entities

                    DevDbSeeder.SeedDevelopmentDataAsync(db).GetAwaiter().GetResult();
                }
                Console.WriteLine("SQLite database created/updated successfully.");
            }
            else
            {
                /*
                 * it's either for local, staging, or very first time in production
                 * because it will apply pending scema changes
                 * and your data might lost from any column
                 */

                //dbContext.Database.Migrate();
                //Console.WriteLine("PostgreSQL database migrated successfully.");
            }

            return app;
        }
    }
}

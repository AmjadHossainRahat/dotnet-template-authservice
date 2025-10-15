using AuthService.API.Services;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Data;

namespace AuthService.API.Seed
{
    public static class DevDbSeeder
    {
        public static async Task SeedDevelopmentDataAsync(ApplicationDbContext context)
        {
            if (!context.Tenants.Any())
            {
                // Seed tenants
                var tenant1 = new Tenant(Guid.Parse("b5644c8f-0558-42df-94cb-e0d04bc2e3f3"), "Tenant One");
                var tenant2 = new Tenant(Guid.Parse("94c01af5-0a43-47ca-bedf-05ab15bc4c43"), "Tenant Two");

                context.Tenants.AddRange(tenant1, tenant2);

                // Seed roles
                var roleAdmin1 = new Role(RoleEnum.SystemAdmin, tenant1.Id);
                var roleOperator1 = new Role(RoleEnum.TenantOperator, tenant1.Id);
                var roleAdmin2 = new Role(RoleEnum.TenantAdmin, tenant2.Id);

                tenant1.AddRole(roleAdmin1);
                tenant1.AddRole(roleOperator1);
                tenant2.AddRole(roleAdmin2);

                var hashedPassword = new PasswordHasher().HashPassword("123456");

                // Seed users
                var user1 = new User("amjad@test.com", "amjad", "+880123456789", hashedPassword, tenant1.Id);
                user1.AssignRole(roleAdmin1);

                var user2 = new User("lamia@test.com", "lamia", null, hashedPassword, tenant1.Id);
                user1.AssignRole(roleAdmin1);

                var user3 = new User("alice@test.com", "alice", null, hashedPassword, tenant1.Id);
                user2.AssignRole(roleOperator1);

                var user4 = new User("bob@test.com", "bob", null, hashedPassword, tenant2.Id);
                user4.AssignRole(roleAdmin2);

                context.Users.AddRange(user1, user2, user3, user4);

                await context.SaveChangesAsync();
                Console.WriteLine("Development seed data created successfully.");
            }
        }
    }
}

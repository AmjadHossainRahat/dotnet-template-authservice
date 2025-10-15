using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Tenant> Tenants { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.Email).IsRequired();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.PhoneNumber).IsRequired(false);

                entity.HasOne(u => u.Tenant)
                      .WithMany(t => t.Users)
                      .HasForeignKey(u => u.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.Roles)
                  .WithMany(r => r.Users)
                  .UsingEntity<Dictionary<string, object>>(
                      "UserRoles", // Name of join table
                      j => j.HasOne<Role>().WithMany().HasForeignKey("RoleId"),
                      j => j.HasOne<User>().WithMany().HasForeignKey("UserId"),
                      j =>
                      {
                          j.HasKey("UserId", "RoleId");
                          j.ToTable("UserRoles");
                      });
            });
            // --- Soft delete filter for Tenant ---
            modelBuilder.Entity<User>().HasQueryFilter(t => !t.IsDeleted);

            // Tenant
            modelBuilder.Entity<Tenant>()
                .HasMany(t => t.Users)
                .WithOne(u => u.Tenant)
                .HasForeignKey(u => u.TenantId);

            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasMany(t => t.Roles)
                      .WithOne(r => r.Tenant)
                      .HasForeignKey(r => r.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // --- Soft delete filter for Tenant ---
            modelBuilder.Entity<Tenant>().HasQueryFilter(t => !t.IsDeleted);

            // Role
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.HasMany(r => r.Permissions)
                      .WithOne(p => p.Role)
                      .HasForeignKey(p => p.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Role>()
                .HasMany(r => r.Users)
                .WithMany(u => u.Roles);

            // Permission
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(p => p.Id);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}

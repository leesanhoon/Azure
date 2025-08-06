using Microsoft.EntityFrameworkCore;
using EnterpriseAuth.Domain.Entities;
using EnterpriseAuth.Infrastructure.Configurations;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseAuth.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply entity configurations
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new RoleConfiguration());
            modelBuilder.ApplyConfiguration(new PermissionConfiguration());
            modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
            modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());

            // Seed default data
            SeedData(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Domain.Common.BaseEntity &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (Domain.Common.BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.Id = Guid.NewGuid();
                }

                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default roles
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var userRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = adminRoleId,
                    Name = "Administrator",
                    Description = "Full system access",
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Role
                {
                    Id = userRoleId,
                    Name = "User",
                    Description = "Standard user access",
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // Seed default permissions
            var permissions = new[]
            {
                new Permission
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "users.read",
                    Description = "Read users",
                    Resource = "users",
                    Action = "read",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Permission
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "users.write",
                    Description = "Write users",
                    Resource = "users",
                    Action = "write",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Permission
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "users.delete",
                    Description = "Delete users",
                    Resource = "users",
                    Action = "delete",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            modelBuilder.Entity<Permission>().HasData(permissions);

            // Assign all permissions to admin role
            modelBuilder.Entity<RolePermission>().HasData(
                new RolePermission
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    RoleId = adminRoleId,
                    PermissionId = permissions[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new RolePermission
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    RoleId = adminRoleId,
                    PermissionId = permissions[1].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new RolePermission
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    RoleId = adminRoleId,
                    PermissionId = permissions[2].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                // User role only gets read permission
                new RolePermission
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    RoleId = userRoleId,
                    PermissionId = permissions[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
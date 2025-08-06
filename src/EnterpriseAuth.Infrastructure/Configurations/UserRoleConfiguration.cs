using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseAuth.Domain.Entities;

namespace EnterpriseAuth.Infrastructure.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("UserRoles");

            builder.HasKey(ur => ur.Id);

            builder.Property(ur => ur.UserId)
                .IsRequired();

            builder.Property(ur => ur.RoleId)
                .IsRequired();

            builder.Property(ur => ur.CreatedAt)
                .IsRequired();

            builder.Property(ur => ur.UpdatedAt)
                .IsRequired();

            builder.Property(ur => ur.CreatedBy)
                .HasMaxLength(50);

            builder.Property(ur => ur.UpdatedBy)
                .HasMaxLength(50);

            // Indexes
            builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
                .IsUnique()
                .HasDatabaseName("IX_UserRoles_UserId_RoleId");

            // Relationships are already defined in User and Role configurations

            // Global query filter for soft delete
            builder.HasQueryFilter(ur => !ur.IsDeleted);
        }
    }
}
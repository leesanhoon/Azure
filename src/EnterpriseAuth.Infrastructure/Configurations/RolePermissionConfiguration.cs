using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseAuth.Domain.Entities;

namespace EnterpriseAuth.Infrastructure.Configurations
{
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermissions");

            builder.HasKey(rp => rp.Id);

            builder.Property(rp => rp.RoleId)
                .IsRequired();

            builder.Property(rp => rp.PermissionId)
                .IsRequired();

            builder.Property(rp => rp.CreatedAt)
                .IsRequired();

            builder.Property(rp => rp.UpdatedAt)
                .IsRequired();

            builder.Property(rp => rp.CreatedBy)
                .HasMaxLength(50);

            builder.Property(rp => rp.UpdatedBy)
                .HasMaxLength(50);

            // Indexes
            builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique()
                .HasDatabaseName("IX_RolePermissions_RoleId_PermissionId");

            // Relationships are already defined in Role and Permission configurations

            // Global query filter for soft delete
            builder.HasQueryFilter(rp => !rp.IsDeleted);
        }
    }
}
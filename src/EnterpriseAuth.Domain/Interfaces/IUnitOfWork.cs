using System;
using System.Threading.Tasks;
using EnterpriseAuth.Domain.Entities;

namespace EnterpriseAuth.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IRepository<Role> Roles { get; }
        IRepository<Permission> Permissions { get; }
        IRepository<UserRole> UserRoles { get; }
        IRepository<RolePermission> RolePermissions { get; }
        IRefreshTokenRepository RefreshTokens { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
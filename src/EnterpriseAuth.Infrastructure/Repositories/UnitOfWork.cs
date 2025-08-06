using Microsoft.EntityFrameworkCore.Storage;
using EnterpriseAuth.Domain.Entities;
using EnterpriseAuth.Domain.Interfaces;
using EnterpriseAuth.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace EnterpriseAuth.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        private IUserRepository? _users;
        private IRepository<Role>? _roles;
        private IRepository<Permission>? _permissions;
        private IRepository<UserRole>? _userRoles;
        private IRepository<RolePermission>? _rolePermissions;
        private IRefreshTokenRepository? _refreshTokens;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IUserRepository Users => _users ??= new UserRepository(_context);

        public IRepository<Role> Roles => _roles ??= new Repository<Role>(_context);

        public IRepository<Permission> Permissions => _permissions ??= new Repository<Permission>(_context);

        public IRepository<UserRole> UserRoles => _userRoles ??= new Repository<UserRole>(_context);

        public IRepository<RolePermission> RolePermissions => _rolePermissions ??= new Repository<RolePermission>(_context);

        public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction started");
            }

            try
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction started");
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
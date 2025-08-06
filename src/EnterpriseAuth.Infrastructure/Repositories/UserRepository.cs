using Microsoft.EntityFrameworkCore;
using EnterpriseAuth.Domain.Entities;
using EnterpriseAuth.Domain.Interfaces;
using EnterpriseAuth.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace EnterpriseAuth.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
        }

        public async Task<User?> GetUserWithRolesAsync(Guid userId)
        {
            return await _dbSet
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserWithRolesAndPermissionsAsync(Guid userId)
        {
            return await _dbSet
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            return await _dbSet
                .AnyAsync(u => u.Username == username);
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _dbSet
                .AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsUsernameExistsAsync(string username, Guid excludeUserId)
        {
            return await _dbSet
                .AnyAsync(u => u.Username == username && u.Id != excludeUserId);
        }

        public async Task<bool> IsEmailExistsAsync(string email, Guid excludeUserId)
        {
            return await _dbSet
                .AnyAsync(u => u.Email == email && u.Id != excludeUserId);
        }
    }
}
using EnterpriseAuth.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace EnterpriseAuth.Domain.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
        Task<User?> GetUserWithRolesAsync(Guid userId);
        Task<User?> GetUserWithRolesAndPermissionsAsync(Guid userId);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<bool> IsEmailExistsAsync(string email);
        Task<bool> IsUsernameExistsAsync(string username, Guid excludeUserId);
        Task<bool> IsEmailExistsAsync(string email, Guid excludeUserId);
    }
}
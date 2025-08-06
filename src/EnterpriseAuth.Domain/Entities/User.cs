using EnterpriseAuth.Domain.Common;
using System;
using System.Collections.Generic;

namespace EnterpriseAuth.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockedOutUntil { get; set; }

        // Navigation properties
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        // Domain methods
        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            FailedLoginAttempts = 0;
        }

        public void IncrementFailedLoginAttempts()
        {
            FailedLoginAttempts++;

            // Lock account after 5 failed attempts for 30 minutes
            if (FailedLoginAttempts >= 5)
            {
                LockedOutUntil = DateTime.UtcNow.AddMinutes(30);
            }
        }

        public bool IsLockedOut()
        {
            return LockedOutUntil.HasValue && LockedOutUntil > DateTime.UtcNow;
        }

        public void UnlockAccount()
        {
            FailedLoginAttempts = 0;
            LockedOutUntil = null;
        }

        public string GetFullName()
        {
            return $"{FirstName} {LastName}".Trim();
        }

        public bool HasRole(string roleName)
        {
            return UserRoles.Any(ur => ur.Role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
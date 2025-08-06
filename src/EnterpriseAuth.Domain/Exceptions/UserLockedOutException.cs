using System;

namespace EnterpriseAuth.Domain.Exceptions
{
    public class UserLockedOutException : DomainException
    {
        public DateTime LockedUntil { get; }

        public UserLockedOutException(DateTime lockedUntil)
            : base($"User account is locked until {lockedUntil:yyyy-MM-dd HH:mm:ss} UTC.")
        {
            LockedUntil = lockedUntil;
        }
    }
}
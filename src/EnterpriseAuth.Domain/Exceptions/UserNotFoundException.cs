namespace EnterpriseAuth.Domain.Exceptions
{
    public class UserNotFoundException : DomainException
    {
        public UserNotFoundException(string identifier)
            : base($"User with identifier '{identifier}' was not found.")
        {
        }
    }
}
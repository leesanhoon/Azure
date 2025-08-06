namespace EnterpriseAuth.Domain.Exceptions
{
    public class InvalidCredentialsException : DomainException
    {
        public InvalidCredentialsException()
            : base("Invalid username or password.")
        {
        }

        public InvalidCredentialsException(string message)
            : base(message)
        {
        }
    }
}
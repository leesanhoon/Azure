using FluentValidation;
using EnterpriseAuth.Application.DTOs;
using System.Text.RegularExpressions;

namespace EnterpriseAuth.Application.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("Username is required.")
                .Length(3, 50)
                .WithMessage("Username must be between 3 and 50 characters.")
                .Matches("^[a-zA-Z0-9_.-]+$")
                .WithMessage("Username can only contain letters, numbers, dots, hyphens, and underscores.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Email address is not valid.")
                .MaximumLength(320)
                .WithMessage("Email must not exceed 320 characters.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required.")
                .Length(8, 255)
                .WithMessage("Password must be between 8 and 255 characters.")
                .Must(BeValidPassword)
                .WithMessage("Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Password confirmation is required.")
                .Equal(x => x.Password)
                .WithMessage("Passwords do not match.");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required.")
                .Length(1, 100)
                .WithMessage("First name must be between 1 and 100 characters.")
                .Matches("^[a-zA-ZÀ-ÿ\\s'-]+$")
                .WithMessage("First name can only contain letters, spaces, hyphens, and apostrophes.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required.")
                .Length(1, 100)
                .WithMessage("Last name must be between 1 and 100 characters.")
                .Matches("^[a-zA-ZÀ-ÿ\\s'-]+$")
                .WithMessage("Last name can only contain letters, spaces, hyphens, and apostrophes.");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .WithMessage("Phone number format is not valid.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        }

        private static bool BeValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            var hasLower = Regex.IsMatch(password, @"[a-z]");
            var hasUpper = Regex.IsMatch(password, @"[A-Z]");
            var hasDigit = Regex.IsMatch(password, @"\d");
            var hasSpecial = Regex.IsMatch(password, @"[@$!%*?&]");

            return hasLower && hasUpper && hasDigit && hasSpecial;
        }
    }
}
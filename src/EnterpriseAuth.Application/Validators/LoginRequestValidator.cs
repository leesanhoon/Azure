using FluentValidation;
using EnterpriseAuth.Application.DTOs;

namespace EnterpriseAuth.Application.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.UsernameOrEmail)
                .NotEmpty()
                .WithMessage("Username or email is required.")
                .Length(3, 320)
                .WithMessage("Username or email must be between 3 and 320 characters.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required.")
                .Length(6, 255)
                .WithMessage("Password must be between 6 and 255 characters.");
        }
    }
}
namespace WebAPI.Validators;

using FluentValidation;

using WebAPI.Endpoints;

public class TournamentDtoValidator : AbstractValidator<TournamentDto>
{
    public TournamentDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description must not be empty.");

        RuleFor(x => x.RegistrationPin)
            .InclusiveBetween(100, 999)
            .When(x => x.RegistrationPin.HasValue)
            .WithMessage("Pin must be between 100 and 999.");
    }
}
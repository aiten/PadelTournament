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
            .Length(5, 5)
            .Matches("^[0-9]{5}$")
            .WithMessage("Pin must be a 5-digit number.");

        RuleFor(x => x.BestOf)
            .GreaterThan(0)
            .WithMessage("BestOf must be greater than 0.")
            .When(x => x.CountType is "Tennis" or "Padel");

        RuleFor(x => x.GamesToWinSet)
            .GreaterThan(0)
            .WithMessage("GamesToWinSet must be greater than 0.")
            .When(x => x.CountType is "Tennis" or "Padel");

        RuleFor(x => x.MinDiff)
            .GreaterThan(0)
            .WithMessage("MinDiff must be greater than 0.")
            .When(x => x.CountType is "Tennis" or "Padel");
    }
}
namespace WebAPI.Validators;

using FluentValidation;

using WebAPI.Endpoints;

public class TeamDtoValidator : AbstractValidator<TeamDto>
{
    public TeamDtoValidator()
    {
        RuleFor(x => x.Player1)
            .NotEmpty()
            .WithMessage("Player1 must not be empty.")
            .MaximumLength(64)
            .WithMessage("Player1 must not exceed 64 characters.");

        RuleFor(x => x.Player2)
            .MaximumLength(64)
            .WithMessage("Player2 must not exceed 64 characters.")
            .When(x => x.Player2 is not null);

        RuleFor(x => x.TournamentId)
            .GreaterThan(0)
            .WithMessage("TournamentId must be greater than 0.");
    }
}
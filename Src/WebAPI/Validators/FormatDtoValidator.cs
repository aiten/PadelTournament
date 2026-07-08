namespace WebAPI.Validators;

using FluentValidation;

using WebAPI.Endpoints;

public class FormatDtoValidator : AbstractValidator<FormatDto>
{
    public FormatDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name must not be empty.");

        RuleFor(x => x.PlayingFormat)
            .Must(v => v is "Tennis" or "Padel" or "Soccer")
            .WithMessage("PlayingFormat must be one of: Tennis, Padel, Soccer.");

        RuleFor(x => x.BestOf)
            .NotNull()
            .GreaterThan(0)
            .WithMessage("BestOf must be greater than 0.")
            .When(x => x.PlayingFormat is "Tennis" or "Padel");

        RuleFor(x => x.GamesToWinSet)
            .NotNull()
            .GreaterThan(0)
            .WithMessage("GamesToWinSet must be greater than 0.")
            .When(x => x.PlayingFormat is "Tennis" or "Padel");

        RuleFor(x => x.MinMargin)
            .NotNull()
            .GreaterThan(0)
            .WithMessage("MinMargin must be greater than 0.")
            .When(x => x.PlayingFormat is "Tennis" or "Padel");
    }
}

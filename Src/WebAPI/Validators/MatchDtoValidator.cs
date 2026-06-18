namespace WebAPI.Validators;

using FluentValidation;

using WebAPI.Endpoints;

public class MatchDtoValidator : AbstractValidator<MatchDto>
{
    public MatchDtoValidator()
    {
        RuleFor(x => x.TournamentId)
            .GreaterThan(0)
            .WithMessage("TournamentId must be greater than 0.");

        RuleFor(x => x.Round)
            .GreaterThan(0)
            .WithMessage("Round must be greater than 0.");

        RuleFor(x => x.No)
            .GreaterThan(0)
            .WithMessage("No must be greater than 0.");
    }
}

public class MatchModifyDtoValidator : AbstractValidator<MatchModifyDto>
{
    public MatchModifyDtoValidator()
    {
        RuleFor(x => x.Remark)
            .MaximumLength(200)
            .WithMessage("Remark must not exceed 200 characters.")
            .When(x => x.Remark is not null);
    }
}
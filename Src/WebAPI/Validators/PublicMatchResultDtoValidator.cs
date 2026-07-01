namespace WebAPI.Validators;

using FluentValidation;

using WebAPI.Endpoints;

public class PublicMatchResultDtoValidator : AbstractValidator<PublicMatchResultDto>
{
    private const string ScorePattern = @"^\s*\d+[:\-]\d+(\(\d+\))?\s*(,\s*\d+[:\-]\d+(\(\d+\))?\s*)*$";

    public PublicMatchResultDtoValidator()
    {
        RuleFor(x => x.Result)
            .Matches(ScorePattern)
            .WithMessage("Result must be a comma-separated list of set scores, e.g. \"6:4, 6:3, 6:5(2)\".")
            .When(x => !string.IsNullOrWhiteSpace(x.Result));
    }
}

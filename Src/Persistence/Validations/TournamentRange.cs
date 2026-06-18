namespace Persistence.Validations;

using System;
using System.ComponentModel.DataAnnotations;

using Persistence.Model;

public class TournamentRange : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        var to         = (DateOnly)value;
        var tournament = (Tournament)validationContext.ObjectInstance;

        if (to <= tournament.From)
        {
            var result = new ValidationResult("To-Time must be after From");
            return result;
        }

        return ValidationResult.Success;
    }
}
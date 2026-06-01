using Core.Entities;

using System.ComponentModel.DataAnnotations;

namespace Core.Validations;

using System;

public class TournamentRange : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        var to        = (DateOnly)value;
        var tournament = (Tournament)validationContext.ObjectInstance;

        if (to <= tournament.From)
        {
            var result = new ValidationResult("To-Time must be after From");
            return result;
        }

        return ValidationResult.Success;
    }
}

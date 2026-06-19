namespace Persistence.Model;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Base.Persistence.Model;

using Persistence.Validations;

public class Tournament : EntityObject
{
    public required string Description { get; set; }

    public DateOnly From { get; set; }

    [TournamentRange]
    public DateOnly? To { get; set; }

    public required string RegistrationPin { get; set; }

    public string? UserId { get; set; }

    public DateTime  Created  { get; set; }
    public DateTime? Modified { get; set; } = null;

    public ICollection<Team>  Teams   { get; set; } = new List<Team>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
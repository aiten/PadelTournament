using Core.Validations;

namespace Core.Entities;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Base.Core.Entities;

public class Tournament : EntityObject
{
    public required string Description { get; set; }

    public DateOnly From { get; set; }

    [TournamentRange]
    public DateOnly? To { get; set; }

    [Range(100, 999)]
    public int? RegistrationPin { get; set; }

    public DateTime  Created  { get; set; }
    public DateTime? Modified { get; set; } = null;

    public ICollection<Team>  Teams   { get; set; } = new List<Team>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();

}

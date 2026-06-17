namespace Persistence.Model;

using System;
using System.Collections.Generic;

using Base.Persistence.Model;

public class Match : EntityObject
{
    public int   Round   { get; set; }
    public int   No      { get; set; }

    public Team? TeamA   { get; set; } = null!;
    public int?  TeamAId { get; set; }

    public Team? TeamB   { get; set; } = null!;
    public int?  TeamBId { get; set; }

    public DateTime? Start { get; set; }

    public Match? NextMatch   { get; set; }
    public int?   NextMatchId { get; set; }

    public Tournament Tournament   { get; set; } = null!;
    public int       TournamentId { get; set; }

    public MatchResult? Result    { get; set; }

    public MatchResult? AcceptA { get; set; }
    
    public MatchResult? AcceptB { get; set; }

    public string? Remark { get; set; }

    public ICollection<Set> Sets { get; set; } = new List<Set>();
}
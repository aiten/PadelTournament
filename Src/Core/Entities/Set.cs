namespace Core.Entities;

using System.Collections.Generic;

using Base.Core.Entities;

public class Set : EntityObject
{
    public int  No             { get; set; }
    public int  ScoreA         { get; set; }
    public int  ScoreB         { get; set; }
    public int? TieBreakPoints { get; set; }

    public Match Match   { get; set; } = null!;
    public int   MatchId { get; set; }

    public ICollection<Game> Games { get; set; } = new List<Game>();
}

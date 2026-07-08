namespace Persistence.Model;

using System.Collections.Generic;

using Base.Persistence.Model;

public class Format : EntityObject
{
    public required string Name { get; set; }

    public PlayingFormat PlayingFormat { get; set; }

    public int? BestOf        { get; set; } = 3;
    public int? GamesToWinSet { get; set; } = 6;
    public int? MinDiff       { get; set; } = 2;
    public bool NoAdv         { get; set; } = false;

    public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
}

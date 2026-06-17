namespace Persistence.QueryResult;

using System.Collections.Generic;

using Persistence.Model;

public record GameResultOverview(
    int     No,
    Server? Server,
    string  Points
);

public record SetResultOverview(
    int                             No,
    int                             ScoreA,
    int                             ScoreB,
    int?                            TieBreakPoints,
    ICollection<GameResultOverview> GameResults
);

public record MatchResultOverview(
    int                            Id,
    string                         PlayerA,
    string                         PlayerB,
    MatchResult?                   Result,
    ICollection<SetResultOverview> SetResults
);
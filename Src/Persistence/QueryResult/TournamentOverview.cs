namespace Persistence.QueryResult;

using System;

public record TournamentOverview(
    int       Id,
    string    Description,
    string    RegistrationPin,
    DateOnly  From,
    DateOnly? To,
    int       Teams,
    int       Matches,
    int       FinishedMatches
);
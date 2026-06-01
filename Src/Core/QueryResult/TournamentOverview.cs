namespace Core.QueryResult;

using System;

public record TournamentOverview(
    int       Id,
    string    Description,
    int?      RegistrationPin,
    DateOnly  From,
    DateOnly? To,
    int       Teams,
    int       Matches,
    int       FinishedMatches
);
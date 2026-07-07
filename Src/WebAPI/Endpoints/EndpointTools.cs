namespace WebAPI.Endpoints;

using System.Collections.Generic;
using System.Linq;

using Persistence.QueryResult;

using Shared.Exceptions;

public static class EndpointTools
{
    public static void CheckId(int id, int dtoId)
    {
        if (id != dtoId)
        {
            throw new IllegalValuesException("The ID in the URL does not match the ID in the request body");
        }
    }

    public static void CheckIdMustBe0(int id)
    {
        if (id != 0)
        {
            throw new IllegalValuesException("The ID in the request body must be 0");
        }
    }

    public static void CheckTournamentId(int tournamentId, int dtoTournamentId)
    {
        if (tournamentId != dtoTournamentId)
        {
            throw new IllegalValuesException("The TournamentId in the body does not match the URL");
        }
    }

    // Parses a comma-separated set score list, e.g. "6:4, 6:3, 6:5(2)", into set results.
    internal static List<SetResultOverview>? ParseSets(string? result)
    {
        if (string.IsNullOrEmpty(result))
        {
            return null;
        }

        return result.Split(',')
            .Select((setScore, index) =>
            {
                var  col         = setScore.Trim().Split('-', ':');
                int? tieBreak    = null;
                int  tieBreakIdx = col[1].IndexOf('(');
                if (tieBreakIdx != -1)
                {
                    tieBreak = int.Parse(col[1].Substring(tieBreakIdx + 1, col[1].IndexOf(')') - tieBreakIdx - 1));
                    col[1]   = col[1].Substring(0, tieBreakIdx);
                }

                return new SetResultOverview(
                    index + 1,
                    int.Parse(col[0]),
                    int.Parse(col[1]),
                    tieBreak,
                    new List<GameResultOverview>());
            })
            .ToList();
    }

}
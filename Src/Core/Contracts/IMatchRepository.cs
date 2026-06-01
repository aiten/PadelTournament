namespace Core.Contracts;

using System.Collections.Generic;
using System.Threading.Tasks;

using Base.Core.Contracts;

using Core.Entities;
using Core.QueryResult;

public interface IMatchRepository : IGenericRepository<Match>
{
    Task<IList<Match>> GetByTournamentAsync(int tournamentId);

    Task<IList<Match>> GetByTeamAsync(int       teamId);

    Task SetWinnerAsync(int matchId, MatchResult winner);

    Task AcceptResultAsync(int matchId, bool forTeamA, MatchResult result);

    Task<MatchResultOverview?> GetMatchResultAsync(int matchId);
    
    Task UpdateMatchResultAsync(int matchId, MatchResultOverview result);
}
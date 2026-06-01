using Core.Entities;

namespace Core.Contracts;

using System.Collections.Generic;
using System.Threading.Tasks;

using Base.Core.Contracts;

using Core.QueryResult;

public interface ITournamentRepository : IGenericRepository<Tournament>
{
    Task<IList<TournamentOverview>> GetTournamentOverviewsAsync();

    Task<Tournament?> GetByPinAsync(int pin);

    Task<Team> RegisterTeamAsync(int tournamentId, string name);

    Task<Team> RegisterTeamByPinAsync(string name, int pin);

    Task<IList<Team>> RegisterTeamsAsync(int tournamentId, IList<(string Name, int? Seed, int? StartMatchPos)> teams);

    Task<Tournament> GenerateMatchSchedule(int tournamentId);

    Task DeleteMatchScheduleAsync(int tournamentId);

    Task DeleteCascadeAsync(int tournamentId);
}
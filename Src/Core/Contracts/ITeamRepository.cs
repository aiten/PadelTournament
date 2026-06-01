namespace Core.Contracts;

using System.Collections.Generic;
using System.Threading.Tasks;

using Base.Core.Contracts;

using Core.Entities;

public interface ITeamRepository : IGenericRepository<Team>
{
    Task<IList<Team>> GetByTournamentAsync(int tournamentId);
    Task<Team?>       GetByRegistrationAsync(int pin, string registrationCode);
}

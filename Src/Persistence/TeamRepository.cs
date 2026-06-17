namespace Persistence;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Base.Persistence;
using Base.Persistence.Contracts;

using Microsoft.EntityFrameworkCore;

using Persistence.Model;

public interface ITeamRepository : IGenericRepository<Team>
{
    Task<IList<Team>> GetByTournamentAsync(int   tournamentId);
    Task<Team?>       GetByRegistrationAsync(int pin, string registrationCode);
}

public class TeamRepository : GenericRepository<Team>, ITeamRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TeamRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IList<Team>> GetByTournamentAsync(int tournamentId)
    {
        return await _dbContext.Teams
            .AsNoTracking()
            .Where(t => t.TournamentId == tournamentId)
            .OrderBy(t => t.Player1).ThenBy(t => t.Player2)
            .ToListAsync();
    }

    public async Task<Team?> GetByRegistrationAsync(int pin, string registrationCode)
    {
        return await _dbContext.Teams
            .AsNoTracking()
            .Include(t => t.Tournament)
            .Where(t => t.RegistrationCode == registrationCode && t.Tournament.RegistrationPin == pin)
            .FirstOrDefaultAsync();
    }
}

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
    Task<Team?> GetByRegistrationAsync(string      pin,          string registrationCode);

    Task<bool>  AnyRegistrationCodePublicAsync(int tournamentId, string code);
}

public class TeamRepository : GenericRepository<Team>, ITeamRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TeamRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Team?> GetByRegistrationAsync(string pin, string registrationCode)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(t => t.Tournament)
            .Where(t => t.RegistrationCode == registrationCode && t.Tournament.RegistrationPin == pin)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> AnyRegistrationCodePublicAsync(int tournamentId, string code)
    {
        return await DbSet.IgnoreQueryFilters().AnyAsync(t => t.TournamentId == tournamentId && t.RegistrationCode == code);
    }

}
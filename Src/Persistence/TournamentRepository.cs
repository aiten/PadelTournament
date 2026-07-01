namespace Persistence;

using Base.Persistence;
using Base.Persistence.Contracts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Persistence.Model;
using Persistence.QueryResult;

public interface ITournamentRepository : IGenericRepository<Tournament>
{
    Task<IList<TournamentOverview>> GetTournamentOverviewsAsync();

    Task<Tournament?> GetByPinAsync(string pin, bool loadTeams = false, bool loadMatches = false);

    Task<bool> AnyRegistrationPinAsync(string pin);
}

public class TournamentRepository : GenericRepository<Tournament>, ITournamentRepository
{
    private readonly ApplicationDbContext          _dbContext;
    private readonly ILogger<TournamentRepository> _logger;

    public TournamentRepository(ApplicationDbContext dbContext, ILogger<TournamentRepository> logger) : base(dbContext)
    {
        _dbContext = dbContext;
        _logger    = logger;
    }

    public async Task<IList<TournamentOverview>> GetTournamentOverviewsAsync()
    {
        return await DbSet
            .Select(t => new TournamentOverview(
                t.Id,
                t.Description,
                t.RegistrationPin,
                t.From,
                t.To,
                t.Teams.Count,
                t.Matches.Count(),
                t.Matches.Count(m => m.Result != null)
            ))
            .ToListAsync();
    }

    public async Task<Tournament?> GetByPinAsync(string pin, bool loadTeams = false, bool loadMatches = false)
    {
        var query = DbSet.IgnoreQueryFilters().Where(t => t.RegistrationPin == pin);
        if (loadTeams)
        {
            query = query.Include(t => t.Teams);
        }

        if (loadMatches)
        {
            query = query.Include(t => t.Matches);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<bool> AnyRegistrationPinAsync(string pin)
    {
        return await DbSet.IgnoreQueryFilters().AnyAsync(t => t.RegistrationPin == pin);
    }
}
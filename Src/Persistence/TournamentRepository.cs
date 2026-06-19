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
    Task<IList<TournamentOverview>> GetTournamentOverviewsAsync(string? userId);

    Task<Tournament?> GetByPinAsync(int pin, bool loadTeams = false, bool loadMatches = false);

    Task<bool> BelongsToUserAsync(int id, string? userId);
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

    public async Task<IList<TournamentOverview>> GetTournamentOverviewsAsync(string? userId)
    {
        var query = _dbContext.Tournaments.AsNoTracking();

        if (userId is not null)
        {
            query = query.Where(t => t.UserId == userId);
        }
        return await query
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

    public async Task<bool> BelongsToUserAsync(int id, string? userId)
    {
        if (userId is null)
        {
            return true;
        }

        return await _dbContext.Tournaments
            .AsNoTracking()
            .AnyAsync(t => t.Id == id && t.UserId == userId);

    }

    public async Task<Tournament?> GetByPinAsync(int pin, bool loadTeams = false, bool loadMatches = false)
    {
        var query = _dbContext.Tournaments.Where(t => t.RegistrationPin == pin);
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
}
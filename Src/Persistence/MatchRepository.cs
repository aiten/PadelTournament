namespace Persistence;

using Base.Persistence;
using Base.Persistence.Contracts;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Persistence.Model;
using Persistence.QueryResult;

using Shared.Exceptions;

public interface IMatchRepository : IGenericRepository<Match>
{
    Task<MatchResultOverview?> GetMatchResultAsync(int matchId);

    // no Tenant
    Task<IList<Match>> GetByTeamNoTenantAsync(int teamId);

    Task<Match?> GetByIdNoTenantAsync(int matchId, params string[]? includeProperties);

}

public class MatchRepository : GenericRepository<Match>, IMatchRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MatchRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MatchResultOverview?> GetMatchResultAsync(int matchId)
    {
        return await DbSet
            .Where(m => m.Id == matchId)
            .Select(m => new MatchResultOverview(
                m.Id,
                m.TeamA!.Player2 != null ? m.TeamA.Player1 + "/" + m.TeamA.Player2 : m.TeamA.Player1,
                m.TeamB!.Player2 != null ? m.TeamB.Player1 + "/" + m.TeamB.Player2 : m.TeamB.Player1,
                m.Result,
                m.Sets.Select(s => new SetResultOverview(
                    s.No,
                    s.ScoreA,
                    s.ScoreB,
                    s.TieBreakPoints,
                    s.Games.Select(g => new GameResultOverview(
                        g.No,
                        g.Server,
                        g.Points)).ToList()
                )).ToList()
            ))
            .SingleOrDefaultAsync();
    }

    #region NoTenant

    public async Task<IList<Match>> GetByTeamNoTenantAsync(int teamId)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.TeamAId == teamId || m.TeamBId == teamId)
            .OrderBy(m => m.Round)
            .ThenBy(m => m.No)
            .ToListAsync();
    }

    public async Task<Match?> GetByIdNoTenantAsync(int matchId, params string[]? includeProperties)
    {
        var query = DbSet
            .IgnoreQueryFilters();

        if (includeProperties != null)
        {
            foreach (string includeProperty in includeProperties!)
            {
                query = query.Include(includeProperty);
            }
        }

        return await query
            .FirstOrDefaultAsync(m => m.Id == matchId);
    }

    #endregion
}
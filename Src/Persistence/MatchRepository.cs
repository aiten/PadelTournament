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

    Task<Match> UpdateMatchResultAsync(int matchId, MatchResultOverview result);

    // no Tenant
    Task<IList<Match>> GetByTeamPublicAsync(int teamId);

    Task<Match?> GetMatchForPublicAsync(int matchId, params string[]? includeProperties);

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

    public async Task<Match> UpdateMatchResultAsync(int matchId, MatchResultOverview result)
    {
        var match = await GetByIdAsync(matchId, nameof(Match.Sets), $"{nameof(Match.Sets)}.{nameof(Set.Games)}", nameof(Match.Tournament));

        if (match is null)
        {
            throw new NotFoundException($"Match not found! {matchId}");
        }

        var toDb = result.SetResults.Select(set => new Set()
        {
            No             = set.No,
            ScoreA         = set.ScoreA,
            ScoreB         = set.ScoreB,
            TieBreakPoints = set.TieBreakPoints,
            Games = set.GameResults.Select(game => new Game()
            {
                No     = game.No,
                Server = game.Server,
                Points = game.Points
            }).ToList()
        }).ToList();

        match.Sets.Sync(toDb, s => s.No, (sDb, sDto) =>
        {
            sDb.ScoreA         = sDto.ScoreA;
            sDb.ScoreB         = sDto.ScoreB;
            sDb.TieBreakPoints = sDto.TieBreakPoints;

            sDb.Games.Sync(sDto.Games, g => g.No, (gDb, gDto) =>
            {
                gDb.Points = gDto.Points;
                gDb.Server = gDto.Server;
            });
        });

        return match;
    }

    #region NoTenant

    public async Task<IList<Match>> GetByTeamPublicAsync(int teamId)
    {
        return await _dbContext.Matches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.TeamAId == teamId || m.TeamBId == teamId)
            .OrderBy(m => m.Round)
            .ThenBy(m => m.No)
            .ToListAsync();
    }

    public async Task<Match?> GetMatchForPublicAsync(int matchId, params string[]? includeProperties)
    {
        var query = _dbContext.Matches
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
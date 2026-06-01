namespace Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Base.Persistence;

using Core.Contracts;
using Core.Entities;
using Core.QueryResult;

using Microsoft.EntityFrameworkCore;

public class MatchRepository : GenericRepository<Match>, IMatchRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MatchRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IList<Match>> GetByTournamentAsync(int tournamentId)
    {
        return await _dbContext.Matches
            .AsNoTracking()
            .Where(m => m.TournamentId == tournamentId)
            .OrderBy(m => m.Round)
            .ThenBy(m => m.No)
            .ToListAsync();
    }

    public async Task<IList<Match>> GetByTeamAsync(int teamId)
    {
        return await _dbContext.Matches
            .AsNoTracking()
            .Where(m => m.TeamAId == teamId || m.TeamBId == teamId)
            .OrderBy(m => m.Round)
            .ThenBy(m => m.No)
            .ToListAsync();
    }

    private async Task<Match> GetActiveMatchAsync(int matchId)
    {
        var match = await GetByIdAsync(matchId, nameof(Match.NextMatch));
        if (match is null)
        {
            throw new InvalidOperationException($"Match nto found! {matchId}");
        }

        if (match.Result != null)
        {
            throw new InvalidOperationException($"The match already ended! {matchId}");
        }

        if (match.TeamAId is null || match.TeamBId is null)
        {
            throw new InvalidOperationException($"Teams not assigned to match");
        }

        return match;
    }

    public async Task SetWinnerAsync(int matchId, MatchResult winner)
    {
        var match = await GetActiveMatchAsync(matchId);

        match.Result = winner;

        var winnerId = winner == MatchResult.WonA ? match.TeamAId : match.TeamBId;

        if (match.NextMatch is not null)
        {
            if ((match.No % 2) == 1)
            {
                if (match.NextMatch.TeamAId is not null)
                {
                    throw new InvalidOperationException($"Teams-A cannot be set (match={match.NextMatch.Id})");
                }

                match.NextMatch.TeamAId = winnerId;
            }
            else
            {
                if (match.NextMatch.TeamBId is not null)
                {
                    throw new InvalidOperationException($"Teams-B cannot be set (match={match.NextMatch.Id})");
                }

                match.NextMatch.TeamBId = winnerId;
            }
        }
    }

    public async Task AcceptResultAsync(int matchId, bool forTeamA, MatchResult result)
    {
        var match = await GetActiveMatchAsync(matchId);

        if (forTeamA)
        {
            match.AcceptA = result;
        }
        else
        {
            match.AcceptB = result;
        }

        if (match.AcceptA == match.AcceptB)
        {
            await SetWinnerAsync(matchId, result);
        }
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

    public async Task UpdateMatchResultAsync(int matchId, MatchResultOverview result)
    {
        var match = await GetByIdAsync(matchId, nameof(Match.Sets), $"{nameof(Match.Sets)}.{nameof(Set.Games)}");

        if (match is null)
        {
            throw new InvalidOperationException($"Match not found! {matchId}");
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
    }
}
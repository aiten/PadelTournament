namespace Service;

using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using Persistence;
using Persistence.Model;
using Persistence.QueryResult;

using Shared.Exceptions;

using System.Threading.Tasks;

using Base.Persistence;

public interface IMatchService
{
    Task<Match> SingleMatchAsync(int id, params string[] includeProperties);

    Task<Match> SingleMatchForTeamAsync(int matchId, int teamId);

    Task UpdateMatchResultAsync(int matchId, MatchResultOverview result);

    Task DeleteMatchResultAsync(int id);

    Task<MatchResultOverview?> GetMatchResultAsync(int matchId);

    Task SetWinnerAsync(int matchId, MatchResult winner);

    Task AcceptResultAsync(int matchId, bool forTeamA, MatchResult result, IList<SetResultOverview>? sets);
}

public class MatchService : IMatchService
{
    private readonly IUnitOfWork             _uow;
    private readonly ILogger<MatchService>   _logger;
    private readonly IHubNotificationService _hub;

    public MatchService(IUnitOfWork uow, ILogger<MatchService> logger, IHubNotificationService hub)
    {
        _uow    = uow;
        _logger = logger;
        _hub    = hub;
    }

    #region REST

    private async Task<Match?> GetMatchByIdAsync(int id, params string[] includeProperties)
    {
        return await _uow.Matches.GetByIdAsync(id, includeProperties);
    }

    public async Task<Match> SingleMatchAsync(int id, params string[] includeProperties)
    {
        return (await GetMatchByIdAsync(id, includeProperties)) ?? throw new NotFoundException($"Match {id} not found");
    }

    public async Task<Match> SingleMatchForTeamAsync(int matchId, int teamId)
    {
        var match = await _uow.Matches.GetByIdNoTenantAsync(matchId) ?? throw new NotFoundException("Match with id {matchId} not found");
        if (match.TeamAId != teamId && match.TeamBId != teamId)
        {
            throw new NotFoundException($"Match {matchId} not found for team {teamId}");
        }

        return match;
    }

    private async Task<Match> CheckMatchAsync(int id, int tournamentId, params string[] includeProperties)
    {
        var entity = await SingleMatchAsync(id, includeProperties);
        if (entity.TournamentId != tournamentId)
        {
            throw new NotFoundException($"Match {id} not found in tournament {tournamentId}");
        }

        return entity;
    }

    public async Task<MatchResultOverview?> GetMatchResultAsync(int matchId)
    {
        return await _uow.Matches.GetMatchResultAsync(matchId);
    }

    public async Task UpdateMatchResultAsync(int matchId, MatchResultOverview result)
    {
        var match = await _uow.Matches.GetByIdNoTenantAsync(matchId, nameof(Match.Sets), $"{nameof(Match.Sets)}.{nameof(Set.Games)}", nameof(Match.Tournament));

        if (match is null)
        {
            throw new NotFoundException($"Match not found! {matchId}");
        }

        UpdateMatchResult(match, result.SetResults.ToList());

        await _uow.SaveChangesAsync();
        await _hub.NotifyTournamentMatchUpdatedAsync(match.Tournament.RegistrationPin, match.Id);
    }

    private bool UpdateMatchResult(Match match, IList<SetResultOverview> sets)
    {
        var toDb = sets.Select(set => new Set()
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

        return true;
    }

    public async Task DeleteMatchResultAsync(int id)
    {
        var entity = await SingleMatchAsync(id, nameof(Match.Sets), nameof(Match.Tournament));
        entity.Sets.Clear();
        await _uow.SaveChangesAsync();
        await _hub.NotifyTournamentMatchUpdatedAsync(entity.Tournament.RegistrationPin, entity.Id);
    }

    #endregion


    public async Task AcceptResultAsync(int matchId, bool forTeamA, MatchResult result, IList<SetResultOverview>? sets)
    {
        bool changed = false;
        var  match   = await GetActiveMatchAsync(matchId);

        if (forTeamA)
        {
            changed       = changed || match.AcceptA != result;
            match.AcceptA = result;
        }
        else
        {
            changed       = changed || match.AcceptB != result;
            match.AcceptB = result;
        }

        if (match.AcceptA == match.AcceptB)
        {
            changed = await SetWinnerAsync(match, result) || changed;
        }

        if (sets is not null)
        {
            changed = UpdateMatchResult(match, sets) || changed;
        }

        await _uow.SaveChangesAsync();
        if (changed)
        {
            await _hub.NotifyTournamentMatchUpdatedAsync(match.Tournament.RegistrationPin, matchId);
        }
    }

    public async Task SetWinnerAsync(int matchId, MatchResult winner)
    {
        var match = await GetActiveMatchAsync(matchId);

        bool changed = await SetWinnerAsync(match, winner);

        await _uow.SaveChangesAsync();
        if (changed)
        {
            await _hub.NotifyTournamentMatchUpdatedAsync(match.Tournament.RegistrationPin, matchId);
        }
    }

    private async Task<bool> SetWinnerAsync(Match match, MatchResult winner)
    {
        bool changed = match.Result != winner;

        match.Result = winner;

        var winnerId = winner == MatchResult.WonA ? match.TeamAId : match.TeamBId;

        if (match.NextMatch is not null)
        {
            if ((match.No % 2) == 1)
            {
                if (match.NextMatch.TeamAId is not null)
                {
                    throw new IllegalValuesException($"Teams-A cannot be set (match={match.NextMatch.Id})");
                }

                changed = changed || match.NextMatch.TeamAId != winnerId;

                match.NextMatch.TeamAId = winnerId;
            }
            else
            {
                if (match.NextMatch.TeamBId is not null)
                {
                    throw new IllegalValuesException($"Teams-B cannot be set (match={match.NextMatch.Id})");
                }

                changed = changed || match.NextMatch.TeamBId != winnerId;

                match.NextMatch.TeamBId = winnerId;
            }
        }

        return changed;
    }

    private async Task<Match> GetActiveMatchAsync(int matchId)
    {
        var match = await _uow.Matches.GetByIdNoTenantAsync(matchId,
            nameof(Match.NextMatch),
            nameof(Match.Tournament),
            nameof(Match.Sets),
            $"{nameof(Match.Sets)}.{nameof(Set.Games)}"
        );
        if (match is null)
        {
            throw new NotFoundException($"Match not found! {matchId}");
        }

        if (match.Result != null)
        {
            throw new IllegalValuesException($"The match already ended! {matchId}");
        }

        if (match.TeamAId is null || match.TeamBId is null)
        {
            throw new IllegalValuesException($"Teams not assigned to match");
        }

        return match;
    }
}
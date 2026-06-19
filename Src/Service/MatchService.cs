namespace Service;

using Microsoft.Extensions.Logging;

using Persistence;
using Persistence.Model;
using Persistence.QueryResult;

using Shared.Exceptions;

using System.Threading.Tasks;

public interface IMatchService
{
    Task<Match>  SingleMatchAsync(int  id, params string[] includeProperties);

    Task<Match> SingleMatchForTeamAsync(int matchId, int teamId, params string[] includeProperties);

    Task UpdateMatchResultAsync(int matchId, MatchResultOverview result);

    Task DeleteMatchResultAsync(int id);

    Task<MatchResultOverview?> GetMatchResultAsync(int matchId);

    Task SetWinnerAsync(int matchId, MatchResult winner);

    Task AcceptResultAsync(int matchId, bool forTeamA, MatchResult result);
}

public class MatchService : IMatchService
{
    private readonly IUnitOfWork             _uow;
    private readonly ILogger<MatchService>   _logger;
    private readonly ICurrentUserService     _currentUserService;
    private readonly IHubNotificationService _hub;

    public MatchService(IUnitOfWork uow, ILogger<MatchService> logger, ICurrentUserService currentUserService, IHubNotificationService hub)
    {
        _uow                = uow;
        _logger             = logger;
        _currentUserService = currentUserService;
        _hub                = hub;
    }

    #region REST

    private async Task<Match?> GetMatchByIdAsync(int id, params string[] includeProperties)
    {
        var entity = await _uow.Matches.GetByIdAsync(id, includeProperties);
        var userId = _currentUserService.IsAdmin ? null : await _currentUserService.GetUserIdAsync();

        if (entity is not null && userId is not null)
        {
            // check if tournament belongs to user (expect admin)
            entity = await _uow.Tournaments.BelongsToUserAsync(entity.TournamentId, userId) ? entity : null;
        }

        return entity;
    }

    public async Task<Match> SingleMatchAsync(int id, params string[] includeProperties)
    {
        return (await GetMatchByIdAsync(id, includeProperties)) ?? throw new NotFoundException($"Match {id} not found");
    }

    public async Task<Match> SingleMatchForTeamAsync(int matchId, int teamId, params string[] includeProperties)
    {
        var match = await SingleMatchAsync(matchId, includeProperties);
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
        var match = await _uow.Matches.UpdateMatchResultAsync(matchId, result);

        await _uow.SaveChangesAsync();
        await _hub.NotifyTournamentMatchUpdatedAsync(match.Tournament.RegistrationPin ?? 0);
    }

    public async Task DeleteMatchResultAsync(int id)
    {
        var entity = await SingleMatchAsync(id, nameof(Match.Sets), nameof(Match.Tournament));
        entity.Sets.Clear();
        await _uow.SaveChangesAsync();
        await _hub.NotifyTournamentMatchUpdatedAsync(entity.Tournament.RegistrationPin ?? 0);
    }

    #endregion


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
            await SetWinnerAsync(match, result);
        }

        await _uow.SaveChangesAsync();
        await _hub.NotifyTournamentMatchUpdatedAsync(match.Tournament.RegistrationPin ?? 0);
    }

    public async Task SetWinnerAsync(int matchId, MatchResult winner)
    {
        var match = await GetActiveMatchAsync(matchId);

        await SetWinnerAsync(match, winner);

        await _uow.SaveChangesAsync();
        await _hub.NotifyTournamentMatchUpdatedAsync(match.Tournament.RegistrationPin ?? 0);
    }

    private async Task SetWinnerAsync(Match match, MatchResult winner)
    {
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

                match.NextMatch.TeamAId = winnerId;
            }
            else
            {
                if (match.NextMatch.TeamBId is not null)
                {
                    throw new IllegalValuesException($"Teams-B cannot be set (match={match.NextMatch.Id})");
                }

                match.NextMatch.TeamBId = winnerId;
            }
        }
    }

    private async Task<Match> GetActiveMatchAsync(int matchId)
    {
        var match = await _uow.Matches.GetByIdAsync(matchId, nameof(Match.NextMatch), nameof(Match.Tournament));
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
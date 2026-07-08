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

using System;

public interface IMatchService
{
    Task<Match> SingleMatchAsync(int id, params string[] includeProperties);

    Task<Match> SingleMatchForTeamAsync(int matchId, int teamId);

    Task UpdateMatchResultAsync(int matchId, MatchResultOverview result);

    Task DeleteMatchResultAsync(int id);

    Task<MatchResultOverview?> GetMatchResultAsync(int matchId);

    Task SetWinnerAsync(int matchId, MatchResult winner, IList<SetResultOverview>? sets = null);

    Task ChangeResultAsync(int matchId, MatchResult winner, IList<SetResultOverview>? sets);

    Task AcceptResultAsync(int matchId, bool forTeamA, MatchResult result, IList<SetResultOverview>? sets);

    Task<IEnumerable<string>> CheckResultAsync(int matchId, MatchResult result, IList<SetResultOverview>? sets);
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
        var entity = await SingleMatchAsync(id, nameof(Match.Sets), nameof(Match.Tournament), nameof(Match.NextMatch));

        if (entity.Result is null)
        {
            return;
        }

        if (entity.NextMatch is not null && entity.NextMatch.Result is not null)
        {
            throw new IllegalValuesException($"Cannot revert the result, the next match already has a result (match={entity.NextMatch.Id})");
        }

        if (entity.NextMatch is not null)
        {
            if ((entity.No % 2) == 1)
            {
                entity.NextMatch.TeamAId = null;
            }
            else
            {
                entity.NextMatch.TeamBId = null;
            }
        }

        entity.Result = null;
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

    public async Task SetWinnerAsync(int matchId, MatchResult winner, IList<SetResultOverview>? sets = null)
    {
        var match = await GetActiveMatchAsync(matchId);

        bool changed = await SetWinnerAsync(match, winner);

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

    public async Task ChangeResultAsync(int matchId, MatchResult winner, IList<SetResultOverview>? sets)
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

        if (match.Result is null)
        {
            throw new IllegalValuesException($"The match has not been played yet! {matchId}");
        }

        bool changed = false;

        if (match.Result != winner)
        {
            if (match.NextMatch is not null && match.NextMatch.Result is not null)
            {
                throw new IllegalValuesException($"Cannot change the winner, the next match already has a result (match={match.NextMatch.Id})");
            }

            match.Result = winner;
            changed      = true;

            var winnerId = winner == MatchResult.WonA ? match.TeamAId : match.TeamBId;

            if (match.NextMatch is not null)
            {
                if ((match.No % 2) == 1)
                {
                    match.NextMatch.TeamAId = winnerId;
                }
                else
                {
                    match.NextMatch.TeamBId = winnerId;
                }
            }
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

    public async Task<IEnumerable<string>> CheckResultAsync(int matchId, MatchResult result, IList<SetResultOverview>? sets)
    {
        if (sets == null || sets.Count == 0)
        {
            return new List<string>();
        }

        var match = await SingleMatchAsync(matchId, $"{nameof(Match.Tournament)}.{nameof(Tournament.Format)}");

        switch (match.Tournament.Format?.PlayingFormat)
        {
            case PlayingFormat.Padel:
            case PlayingFormat.Tennis:
                return CheckResultTennis(
                    match.Tournament.Format.BestOf ?? 3,
                    match.Tournament.Format.GamesToWinSet ?? 6,
                    match.Tournament.Format.MinDiff ?? 2,
                    result, sets);
            case PlayingFormat.Soccer:
                return CheckResultSoccer(result, sets);
            default:
                return new List<string>();
        }
    }

    private IEnumerable<string> CheckResultTennis(int bestOf, int minGamesToWinSet, int minDiff, MatchResult result, IList<SetResultOverview> sets)
    {
        int minWin           = bestOf / 2 + 1; // int div
        int maxGamesToWinSet = minGamesToWinSet + minDiff - 1;
        int tiebreakMinDiff  = 2;

        string teamName = result == MatchResult.WonA ? "Team A" : "Team B";

        var errors = sets.SelectMany((set, index) =>
        {
            var err = new List<string>();

            int  maxScore     = Math.Max(set.ScoreA, set.ScoreB);
            int  minScore     = Math.Min(set.ScoreA, set.ScoreB);
            bool needTiebreak = false;

            if (Math.Abs(set.ScoreB - set.ScoreA) < minDiff && (minDiff < 2 || maxScore != maxGamesToWinSet))
            {
                err.Add($"Set {index + 1}: Difference must be greater or equal {minDiff}");
            }

            if (maxScore < minGamesToWinSet)
            {
                err.Add($"Set {index + 1}: Won games must be greater or equal to {minGamesToWinSet}");
            }

            if (maxScore > maxGamesToWinSet)
            {
                err.Add($"Set {index + 1}: Won games must be less or equal to {maxGamesToWinSet}");
            }

            if (minDiff > 1)
            {
                if (maxScore == maxGamesToWinSet && maxScore - minScore > minDiff)
                {
                    err.Add($"Set {index + 1}: Won sets cannot be {maxGamesToWinSet} if other has less than {maxGamesToWinSet - minDiff}");
                }

                if (maxScore == maxGamesToWinSet && minScore == maxGamesToWinSet - 1)
                {
                    needTiebreak = true;
                    if ((set.TieBreakPoints ?? 0) < tiebreakMinDiff)
                    {
                        err.Add($"Set {index + 1}: Tiebreak points must be >= {tiebreakMinDiff}");
                    }
                }
            }

            if (needTiebreak == false && set.TieBreakPoints is not null)
            {
                err.Add($"Set {index + 1}: Tiebreak not allowed {set.TieBreakPoints ?? 0}");
            }

            return err;
        }).ToList();
        ;

        int wonASets = sets.Count(s => s.ScoreA > s.ScoreB);
        int wonBSets = sets.Count(s => s.ScoreA < s.ScoreB);

        if (wonASets + wonBSets > bestOf)
        {
            errors.Add($"Invalid number of sets ({wonASets + wonBSets}). Must be less than or equal to {bestOf}");
        }
        else if ((wonASets != minWin && result == MatchResult.WonA) || (wonBSets != minWin && result == MatchResult.WonB))
        {
            errors.Add($"Invalid match result. {teamName} did not win the required number of sets ({minWin}).");
        }

        var last = sets.Last();
        if (result == MatchResult.WonA && last.ScoreA < last.ScoreB ||
            result == MatchResult.WonB && last.ScoreA > last.ScoreB)
        {
            errors.Add($"Invalid match result. {teamName} did not win the last set.");
        }

        return errors;
    }

    private IEnumerable<string> CheckResultSoccer(MatchResult result, IList<SetResultOverview> sets)
    {
        string teamName = result == MatchResult.WonA ? "Team A" : "Team B";

        int scoreA = 0;
        int scoreB = 0;

        var errors = sets.SelectMany((set, index) =>
        {
            var err = new List<string>();

            if (scoreA > set.ScoreA || scoreB > set.ScoreB)
            {
                err.Add($"Set {index + 1}: Goals cannot be revoked!");
            }

            scoreA = set.ScoreA;
            scoreB = set.ScoreB;

            if (set.TieBreakPoints is not null)
            {
                err.Add($"Set {index + 1}: Soccer has no tiebreak");
            }

            return err;
        }).ToList();

        var last     = sets.Last();
        if (result == MatchResult.WonA && last.ScoreA < last.ScoreB ||
            result == MatchResult.WonB && last.ScoreA > last.ScoreB)
        {
            errors.Add($"Invalid match result. {teamName} did not win.");
        }

        return errors;
    }
}
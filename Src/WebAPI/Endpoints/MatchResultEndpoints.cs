namespace WebAPI.Endpoints;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Persistence;
using Persistence.Model;
using Persistence.QueryResult;

using Service;

public record MatchResultDto(
    int                Id,
    string?            Result,
    List<SetResultDto> Sets
);

public record SetResultDto(
    int          No,
    string       Result,
    List<string> Games
);

public static class MatchResultEndpoints
{
    #region Dto-Entity Mapping

    private static MatchResultDto? ToDto(MatchResultOverview? entity)
    {
        if (entity is null) return null;

        return new MatchResultDto(
            entity.Id,
            entity.Result.ToString(),
            entity.SetResults.Select(set =>
            {
                string tieBreak = set.TieBreakPoints.HasValue ? $"({set.TieBreakPoints})" : "";
                return new SetResultDto(
                    set.No,
                    $"{set.ScoreA}-{set.ScoreB}{tieBreak}",
                    set.GameResults.Select(game => $"{(game.Server is null ? null : game.Server + ":")}{game.Points}").ToList()
                );
            }).ToList()
        );
    }

    private static MatchResultOverview? ToEntity(MatchResultDto? dto)
    {
        if (dto is null) return null;

        return new MatchResultOverview(
                dto.Id,
                string.Empty,
                string.Empty,
                string.IsNullOrEmpty(dto.Result) ? null : Enum.Parse<MatchResult>(dto.Result),
                dto.Sets.Select(set =>
                {
                    var  col            = set.Result.Split('-',':');
                    int? tieBreakPoints = null;
                    int  tieBreakIdx    = col[1].IndexOf('(');
                    if (tieBreakIdx != -1)
                    {
                        tieBreakPoints = int.Parse(col[1].Substring(tieBreakIdx + 1, col[1].IndexOf(')') - tieBreakIdx - 1));
                        col[1]         = col[1].Substring(0, tieBreakIdx);
                    }

                    return new SetResultOverview(
                        set.No,
                        int.Parse(col[0]),
                        int.Parse(col[1]),
                        tieBreakPoints,
                        set.Games
                            .Select((gameScore, gameIndex) =>
                            {
                                var colGame = gameScore.Split(':');
                                return new GameResultOverview(
                                    gameIndex + 1,
                                    colGame.Length > 1 ? Enum.Parse<Server>(colGame[0]) : null,
                                    colGame[^1]);
                            }

                            ).ToList());
                }).ToList()
            );
    }

    #endregion

    public static void MapMatchResultEndpoints(this IEndpointRouteBuilder app, string baseRoute)
    {
        var routeRead = app
            .MapGroup($"{baseRoute}/{{tournamentId:int}}/matches/{{matchId:int}}/result")
            .WithTags("MatchResults")
            .RequireAuthorization(Settings.AdminOrUserPolicyName)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var routeAdmin = app
            .MapGroup($"{baseRoute}/{{tournamentId:int}}/matches/{{matchId:int}}/result")
            .WithTags("MatchResults")
            .RequireAuthorization(Settings.AdminPolicyName);

        routeRead.MapGet("", async (int tournamentId, int matchId, IUnitOfWork uow) =>
            {
                var match = await uow.Matches.GetByIdAsync(matchId);

                if (match is null || match.TournamentId != tournamentId)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Match not found",
                        detail: $"No match found with ID {matchId} in tournament {tournamentId}");
                }

                return Results.Ok(ToDto(await uow.Matches.GetMatchResultAsync(matchId)));
            })
            .WithName("GetMatchResult")
            .Produces<MatchResultDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);


        routeAdmin.MapPut("", async (int tournamentId, int matchId, MatchResultDto dto, IUnitOfWork uow, IHubNotificationService hub) =>
            {
                var match = await uow.Matches.GetByIdAsync(matchId);

                if (match is null || match.TournamentId != tournamentId)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Match not found",
                        detail: $"No match found with ID {matchId} in tournament {tournamentId}");
                }

                using var trans = await uow.BeginTransactionAsync();

                var result = ToEntity(dto);
                await uow.Matches.UpdateMatchResultAsync(matchId, result!);
                await trans.CommitTransactionAsync();
                await hub.NotifyTournamentMatchUpdatedAsync(tournamentId);

                return Results.NoContent();
            })
            .WithName("UpdateMatchResult")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeAdmin.MapDelete("", async (int tournamentId, int matchId, IUnitOfWork uow, IHubNotificationService hub) =>
            {
                var match = await uow.Matches.GetByIdAsync(matchId, nameof(Match.Sets));

                if (match is null || match.TournamentId != tournamentId)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Match not found",
                        detail: $"No match found with ID {matchId} in tournament {tournamentId}");
                }

                using var trans = await uow.BeginTransactionAsync();
                match.Sets.Clear();
                await trans.CommitTransactionAsync();
                await hub.NotifyTournamentMatchUpdatedAsync(tournamentId);

                return Results.NoContent();
            })
            .WithName("DeleteMatchResult")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
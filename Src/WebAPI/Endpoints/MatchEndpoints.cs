namespace WebAPI.Endpoints;

using System;
using System.Collections.Generic;
using System.Linq;

using Base.Persistence.Contracts;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Persistence;
using Persistence.Model;
using Persistence.QueryResult;

using Service;

using WebAPI.Filters;

public record MatchDto(
    int                       Id,
    int                       TournamentId,
    int                       Round,
    int                       No,
    int?                      TeamAId,
    int?                      TeamBId,
    DateTime?                 Start,
    int?                      NextMatchId,
    string?                   Result,
    string?                   AcceptA,
    string?                   AcceptB,
    string?                   Remark,
    IList<SetResultOverview>? Sets);

public record MatchModifyDto(
    int?      TeamAId,
    int?      TeamBId,
    DateTime? Start,
    int?      NextMatchId,
    string?   Result,
    string?   Remark
);

public record SetWinnerDto(string Winner, string? Result = null);

public static class MatchEndpoints
{
    #region Dto-Entity Mapping

    public static SetResultOverview ToDto(Set entity)
    {
        return new SetResultOverview(
            entity.Id,
            entity.ScoreA,
            entity.ScoreB,
            entity.TieBreakPoints,
            new List<GameResultOverview>()
        );
    }

    public static MatchDto? ToDto(Match? entity)
    {
        if (entity is null) return null;

        return new MatchDto(
            entity.Id,
            entity.TournamentId,
            entity.Round,
            entity.No,
            entity.TeamAId,
            entity.TeamBId,
            entity.Start,
            entity.NextMatchId,
            entity.Result?.ToString(),
            entity.AcceptA?.ToString(),
            entity.AcceptB?.ToString(),
            entity.Remark,
            entity.Sets?.Select(ToDto).ToList()
        );
    }

    #endregion

    public static void MapMatchEndpoints(this IEndpointRouteBuilder app, string baseRoute)
    {
        var routeUser = app
            .MapGroup($"{baseRoute}/{{tournamentId:int}}/matches")
            .WithTags("Matches")
            .RequireAuthorization(Settings.AdminOrUserPolicyName)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        /*
        var routeAdmin = app
            .MapGroup($"{baseRoute}/{{tournamentId:int}}/matches")
            .WithTags("Matches")
            .RequireAuthorization(Settings.AdminPolicyName);
        */

        routeUser.MapGet("", async (int tournamentId, ITournamentService tournamentService) =>
            {
                var tournament = await tournamentService.SingleTournamentAsync(tournamentId, $"{nameof(Tournament.Matches)}.{nameof(Match.Sets)}");
                return Results.Ok(tournament.Matches.Select(ToDto).ToList());
            })
            .WithName("GetMatches")
            .Produces<List<MatchDto>>(StatusCodes.Status200OK);

        routeUser.MapGet("/{id:int}", async (int tournamentId, int id, IMatchService matchService) =>
            {
                var entity = await matchService.SingleMatchAsync(id, nameof(Match.Sets));
                EndpointTools.CheckTournamentId(tournamentId, entity.TournamentId);

                return Results.Ok(ToDto(entity));
            })
            .WithName("GetMatch")
            .Produces<MatchDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeUser.MapPut("/{id:int}", async (int tournamentId, int id, MatchModifyDto dto, IMatchService matchService, ITransactionProvider transactionProvider) =>
            {
                using var trans = await transactionProvider.BeginTransactionAsync();

                var entity = await matchService.SingleMatchAsync(id);
                EndpointTools.CheckTournamentId(tournamentId, entity.TournamentId);

                entity.TeamAId     = dto.TeamAId;
                entity.TeamBId     = dto.TeamBId;
                entity.Start       = dto.Start;
                entity.NextMatchId = dto.NextMatchId;
                entity.Result      = EntityResult(dto.Result);
                entity.Remark      = dto.Remark;

                //TODO : Validate the entity before saving changes, Notify the clients about the changes, etc.
                await trans.CommitTransactionAsync();

                return Results.NoContent();
            })
            .WithValidation<MatchModifyDto>()
            .WithName("ModifyMatch")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeUser.MapPut("/{id:int}/winner", async (int tournamentId, int id, SetWinnerDto dto, IMatchService matchService, ITransactionProvider transactionProvider) =>
            {
                var match = await matchService.SingleMatchAsync(id);
                EndpointTools.CheckTournamentId(tournamentId, match.TournamentId);

                var winner = EntityResult(dto.Winner);

                if (winner is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid winner",
                        detail: "Winner must be 'WonA' or 'WonB'");
                }

                using var trans = await transactionProvider.BeginTransactionAsync();
                var sets = EndpointTools.ParseSets(dto.Result);

                if (match.Result is null)
                {
                    await matchService.SetWinnerAsync(id, winner.Value, sets);
                }
                else
                {
                    await matchService.ChangeResultAsync(id, winner.Value, sets);
                }

                await trans.CommitTransactionAsync();

                return Results.NoContent();
            })
            .WithName("SetMatchWinner")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeUser.MapPut("/{id:int}/checkresult", async (int tournamentId, int id, SetWinnerDto dto, IMatchService matchService, ITransactionProvider transactionProvider) =>
            {
                var match = await matchService.SingleMatchAsync(id);
                EndpointTools.CheckTournamentId(tournamentId, match.TournamentId);

                var winner = EntityResult(dto.Winner);

                if (winner is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid winner",
                        detail: "Winner must be 'WonA' or 'WonB'");
                }

                using var trans = await transactionProvider.BeginTransactionAsync();
                var       sets  = EndpointTools.ParseSets(dto.Result);

                var errors = await matchService.CheckResultAsync(id, winner.Value, sets);

                await trans.CommitTransactionAsync();

                return Results.Ok(errors);
            })
            .WithName("CheckMatchWinner")
            .Produces<List<string>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

    }

    private static MatchResult? EntityResult(string? result)
    {
        return result switch
        {
            "WonA" => MatchResult.WonA,
            "WonB" => MatchResult.WonB,
            _      => null
        };
    }
}
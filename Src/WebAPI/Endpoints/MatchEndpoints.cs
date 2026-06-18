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

using Service;

using WebAPI.Filters;

public record MatchDto(
    int       Id,
    int       TournamentId,
    int       Round,
    int       No,
    int?      TeamAId,
    int?      TeamBId,
    DateTime? Start,
    int?      NextMatchId,
    string?   Result,
    string?   AcceptA,
    string?   AcceptB,
    string?   Remark
);

public record MatchModifyDto(
    int?      TeamAId,
    int?      TeamBId,
    DateTime? Start,
    int?      NextMatchId,
    string?   Result,
    string?   Remark
);

public record SetWinnerDto(string Winner);

public static class MatchEndpoints
{
    #region Dto-Entity Mapping

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
            entity.Remark);
    }

    #endregion

    public static void MapMatchEndpoints(this IEndpointRouteBuilder app, string baseRoute)
    {
        var routeRead = app
            .MapGroup($"{baseRoute}/{{tournamentId:int}}/matches")
            .WithTags("Matches")
            .RequireAuthorization(Settings.AdminOrUserPolicyName)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var routeAdmin = app
            .MapGroup($"{baseRoute}/{{tournamentId:int}}/matches")
            .WithTags("Matches")
            .RequireAuthorization(Settings.AdminPolicyName);

        routeRead.MapGet("", async (int tournamentId, ITournamentService tournamentService) =>
            {
                var tournament = await tournamentService.SingleTournamentAsync(tournamentId, nameof(Tournament.Matches));
                return Results.Ok(tournament.Matches.Select(ToDto).ToList());
            })
            .WithName("GetMatches")
            .Produces<List<MatchDto>>(StatusCodes.Status200OK);

        routeRead.MapGet("/{id:int}", async (int tournamentId, int id, IMatchService matchService) =>
            {
                var entity = await matchService.SingleMatchAsync(id);
                EndpointTools.CheckTournamentId(tournamentId, entity.TournamentId);

                return Results.Ok(ToDto(entity));
            })
            .WithName("GetMatch")
            .Produces<MatchDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeAdmin.MapPost("", async (int tournamentId, MatchDto dto, IMatchService matchService, ITransactionProvider transactionProvider) =>
            {
                EndpointTools.CheckIdMustBe0(dto.Id);
                EndpointTools.CheckTournamentId(tournamentId, dto.TournamentId);

                using var trans = await transactionProvider.BeginTransactionAsync();

                var entity = new Match
                {
                    TournamentId = tournamentId,
                    Round        = dto.Round,
                    No           = dto.No,
                    TeamAId      = dto.TeamAId,
                    TeamBId      = dto.TeamBId,
                    Start        = dto.Start,
                    NextMatchId  = dto.NextMatchId,
                    Remark       = dto.Remark
                };

                await matchService.AddMatchAsync(entity);

                int id = entity.Id;
                return Results.Created(
                    $"{baseRoute}/{tournamentId}/matches/{id}",
                    ToDto(await matchService.GetMatchByIdAsync(id)));
            })
            .WithValidation<MatchDto>()
            .WithName("AddMatch")
            .Produces<MatchDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeAdmin.MapPut("/{id:int}", async (int tournamentId, int id, MatchModifyDto dto, IMatchService matchService, ITransactionProvider transactionProvider) =>
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

        routeAdmin.MapPut("/{id:int}/winner", async (int tournamentId, int id, SetWinnerDto dto, IMatchService matchService, ITransactionProvider transactionProvider) =>
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
                await matchService.SetWinnerAsync(id, winner.Value);
                await trans.CommitTransactionAsync();

                return Results.NoContent();
            })
            .WithName("SetMatchWinner")
            .Produces(StatusCodes.Status204NoContent)
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
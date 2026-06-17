namespace WebAPI.Endpoints;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Persistence;
using Persistence.Model;

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

        routeRead.MapGet("", async (int tournamentId, IUnitOfWork uow) =>
            {
                var matches = await uow.Matches.GetByTournamentAsync(tournamentId);
                return Results.Ok(matches.Select(ToDto).ToList());
            })
            .WithName("GetMatches")
            .Produces<List<MatchDto>>(StatusCodes.Status200OK);

        routeRead.MapGet("/{id:int}",  async (int tournamentId, int id, IUnitOfWork uow) =>
            {
                var entity = await uow.Matches.GetByIdAsync(id);

                if (entity is null || entity.TournamentId != tournamentId)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Match not found",
                        detail: $"No match found with ID {id} in tournament {tournamentId}");
                }

                return Results.Ok(ToDto(entity));
            })
            .WithName("GetMatch")
            .Produces<MatchDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeAdmin.MapPost("", async (int tournamentId, MatchDto dto, IUnitOfWork uow) =>
            {
                if (dto.Id != 0)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid request",
                        detail: "The ID in the request body must be 0");
                }

                if (dto.TournamentId != tournamentId)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid request",
                        detail: "The TournamentId in the body does not match the URL");
                }

                using var trans = await uow.BeginTransactionAsync();

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

                await uow.Matches.AddAsync(entity);
                await trans.CommitTransactionAsync();

                int id = entity.Id;
                return Results.Created(
                    $"{baseRoute}/{tournamentId}/matches/{id}",
                    ToDto(await uow.Matches.GetByIdAsync(id)));
            })
            .WithValidation<MatchDto>()
            .WithName("AddMatch")
            .Produces<MatchDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeAdmin.MapPut("/{id:int}", async (int tournamentId, int id, MatchModifyDto dto, IUnitOfWork uow) =>
            {
                using var trans = await uow.BeginTransactionAsync();

                var entity = await uow.Matches.GetByIdAsync(id);
                if (entity is null || entity.TournamentId != tournamentId)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Match not found",
                        detail: $"No match found with ID {id} in tournament {tournamentId}");
                }

                entity.TeamAId     = dto.TeamAId;
                entity.TeamBId     = dto.TeamBId;
                entity.Start       = dto.Start;
                entity.NextMatchId = dto.NextMatchId;
                entity.Result      = EntityResult(dto.Result);
                entity.Remark      = dto.Remark;

                await trans.CommitTransactionAsync();

                return Results.NoContent();
            })
            .WithValidation<MatchModifyDto>()
            .WithName("ModifyMatch")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeAdmin.MapPut("/{id:int}/winner", async (int tournamentId, int id, SetWinnerDto dto, IUnitOfWork uow) =>
            {
                var match = await uow.Matches.GetByIdAsync(id);
                if (match is null || match.TournamentId != tournamentId)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Match not found",
                        detail: $"No match found with ID {id} in tournament {tournamentId}");
                }

                var winner = EntityResult(dto.Winner);

                if (winner is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid winner",
                        detail: "Winner must be 'WonA' or 'WonB'");
                }

                try
                {
                    using var trans = await uow.BeginTransactionAsync();
                    await uow.Matches.SetWinnerAsync(id, winner.Value);
                    await trans.CommitTransactionAsync();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Cannot set winner",
                        detail: ex.Message);
                }

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
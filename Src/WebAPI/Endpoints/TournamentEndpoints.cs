namespace WebAPI.Endpoints;

using System;
using System.Collections.Generic;
using System.Linq;

using Core.Contracts;
using Core.Entities;
using Core.QueryResult;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using WebAPI.Filters;

public record TournamentDto(
    int       Id,
    string    Description,
    DateOnly  From,
    DateOnly? To,
    int?      RegistrationPin
);

public static class TournamentEndpoints
{
    #region Dto-Entity Mapping

    private static Tournament ToEntity(TournamentDto dto)
    {
        return new Tournament()
        {
            Id              = dto.Id,
            Description     = dto.Description,
            From            = dto.From,
            To              = dto.To,
            RegistrationPin = dto.RegistrationPin
        };
    }

    public static TournamentDto? ToDto(Tournament? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new TournamentDto(
            entity.Id,
            entity.Description,
            entity.From,
            entity.To,
            entity.RegistrationPin
        );
    }

    private static IList<TournamentDto>? ToDto(IList<Tournament>? list)
    {
        if (list is null)
        {
            return null;
        }

        return list.Select(x => ToDto(x)!).ToList();
    }

    #endregion


    public static void MapTournamentEndpoints(this IEndpointRouteBuilder app, string baseRoute)
    {
        var route = app
            .MapGroup(baseRoute)
            .WithTags("Tournament")
            .RequireAuthorization(Settings.AdminOrUserPolicyName)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var routeAdmin = app
            .MapGroup(baseRoute)
            .WithTags("Tournament")
            .RequireAuthorization(Settings.AdminPolicyName);

        route.MapGet("", async (IUnitOfWork uow) =>
            {
                var dtos = await uow.Tournaments.GetTournamentOverviewsAsync();
                return Results.Ok(dtos);
            })
            .WithName("GetTournaments")
            .Produces<List<TournamentOverview>>(StatusCodes.Status200OK);


        route.MapGet("/{id:int}", async (int id, IUnitOfWork uow) =>
            {
                var dto = ToDto(await uow.Tournaments.GetByIdAsync(id));

                if (dto is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Tournament not found",
                        detail: $"No Tournament found with ID {id}");
                }

                return Results.Ok(dto);
            })
            .WithName("GetTournament")
            .Produces<TournamentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeAdmin.MapPut("/{id:int}", async (int id, TournamentDto dto, IUnitOfWork uow) =>
            {
                if (id != dto.Id)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid request",
                        detail: "The ID in the URL does not match the ID in the request body");
                }

                using (var trans = await uow.BeginTransactionAsync())
                {
                    var entity = await uow.Tournaments.GetByIdAsync(id);

                    if (entity is null)
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Tournament not found",
                            detail: $"No Tournament found with ID {id}");
                    }

                    entity.Description     = dto.Description;
                    entity.RegistrationPin = dto.RegistrationPin;
                    entity.From            = dto.From;
                    entity.To              = dto.To;
                    entity.Modified        = DateTime.Now;

                    await trans.CommitTransactionAsync();
                }

                return Results.NoContent();
            })
            .WithValidation<TournamentDto>()
            .WithName("UpdateTournament")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);


        routeAdmin.MapPost("", async (TournamentDto dto, IUnitOfWork uow) =>
            {
                if (dto.Id != 0)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid request",
                        detail: "The ID in the request body must be 0");
                }

                using (var trans = await uow.BeginTransactionAsync())
                {
                    var entity = ToEntity(dto);

                    entity.Created = DateTime.Now;

                    await uow.Tournaments.AddAsync(entity);

                    await trans.CommitTransactionAsync();

                    int id = entity.Id;

                    return Results.Created($"{baseRoute}/{id}", ToDto(await uow.Tournaments.GetByIdAsync(id)));
                }
            })
            .WithValidation<TournamentDto>()
            .WithName("AddTournament")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeAdmin.MapDelete("/{id:int}", async (int id, IUnitOfWork uow) =>
            {
                using var trans = await uow.BeginTransactionAsync();

                try
                {
                    await uow.Tournaments.DeleteCascadeAsync(id);
                    await trans.CommitTransactionAsync();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Cannot delete tournament",
                        detail: ex.Message);
                }

                return Results.NoContent();
            })
            .WithName("DeleteTournament")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status204NoContent);

        routeAdmin.MapPost("/{id:int}/generate-schedule", async (int id, IUnitOfWork uow) =>
            {
                using var trans = await uow.BeginTransactionAsync();

                Tournament tournament;
                try
                {
                    tournament = await uow.Tournaments.GenerateMatchSchedule(id);
                    await trans.CommitTransactionAsync();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Cannot generate bracket",
                        detail: ex.Message);
                }

                return Results.Ok(tournament.Matches.Select(MatchEndpoints.ToDto).ToList());
            })
            .WithName("GenerateBracket")
            .Produces<List<MatchDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeAdmin.MapDelete("/{id:int}/revert-schedule", async (int id, IUnitOfWork uow) =>
            {
                using var trans = await uow.BeginTransactionAsync();

                try
                {
                    await uow.Tournaments.DeleteMatchScheduleAsync(id);
                    await trans.CommitTransactionAsync();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Cannot revert schedule",
                        detail: ex.Message);
                }

                return Results.NoContent();
            })
            .WithName("RevertBracket")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
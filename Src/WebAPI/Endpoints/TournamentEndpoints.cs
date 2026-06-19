namespace WebAPI.Endpoints;

using System;
using System.Collections.Generic;
using System.Linq;

using Base.Persistence.Contracts;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Persistence.Model;
using Persistence.QueryResult;

using Service;

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
        var routeUser = app
            .MapGroup(baseRoute)
            .WithTags("Tournament")
            .RequireAuthorization(Settings.AdminOrUserPolicyName)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var routeAdmin = app
            .MapGroup(baseRoute)
            .WithTags("Tournament")
            .RequireAuthorization(Settings.AdminPolicyName)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routeUser.MapGet("", async (ITournamentService tournamentService) =>
            {
                var dtos = await tournamentService.GetTournamentOverviewsAsync();
                return Results.Ok(dtos);
            })
            .WithName("GetTournaments")
            .Produces<List<TournamentOverview>>(StatusCodes.Status200OK);


        routeUser.MapGet("/{id:int}", async (int id, ITournamentService tournamentService) =>
            {
                var dto = ToDto(await tournamentService.SingleTournamentAsync(id));
                return Results.Ok(dto);
            })
            .WithName("GetTournament")
            .Produces<TournamentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeUser.MapPut("/{id:int}", async (int id, TournamentDto dto, ITournamentService tournamentService, ITransactionProvider transactionProvider) =>
            {
                EndpointTools.CheckId(id, dto.Id);

                using var trans = await transactionProvider.BeginTransactionAsync();

                await tournamentService.UpdateTournamentAsync(id, ToEntity(dto));
                await trans.CommitTransactionAsync();

                return Results.NoContent();
            })
            .WithValidation<TournamentDto>()
            .WithName("UpdateTournament")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);


        routeUser.MapPost("", async (TournamentDto dto, ITournamentService service, ITransactionProvider transactionProvider) =>
            {
                EndpointTools.CheckIdMustBe0(dto.Id);

                using var trans = await transactionProvider.BeginTransactionAsync();

                var entity  = ToEntity(dto);
                var created = await service.AddTournamentAsync(entity);

                await trans.CommitTransactionAsync();

                return Results.Created($"{baseRoute}/{created.Id}", ToDto(created));
            })
            .WithValidation<TournamentDto>()
            .WithName("AddTournament")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeUser.MapDelete("/{id:int}", async (int id, ITournamentService service, ITransactionProvider transactionProvider) =>
            {
                using var trans = await transactionProvider.BeginTransactionAsync();

                await service.DeleteTournamentAsync(id);
                await trans.CommitTransactionAsync();

                return Results.NoContent();
            })
            .WithName("DeleteTournament")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status204NoContent);

        routeUser.MapPost("/{id:int}/generate-schedule", async (int id, ITournamentService service, ITransactionProvider transactionProvider) =>
            {
                using var trans = await transactionProvider.BeginTransactionAsync();

                var tournament = await service.GenerateMatchScheduleAsync(id);
                await trans.CommitTransactionAsync();

                return Results.Ok(tournament.Matches.Select(MatchEndpoints.ToDto).ToList());
            })
            .WithName("GenerateBracket")
            .Produces<List<MatchDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeUser.MapDelete("/{id:int}/revert-schedule", async (int id, ITournamentService service, ITransactionProvider transactionProvider) =>
            {
                using var trans = await transactionProvider.BeginTransactionAsync();

                await service.DeleteMatchScheduleAsync(id);
                await trans.CommitTransactionAsync();

                return Results.NoContent();
            })
            .WithName("RevertBracket")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
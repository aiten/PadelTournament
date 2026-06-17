namespace WebAPI.Endpoints;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Persistence;
using Persistence.Model;

using Service;

using WebAPI.Filters;

public record TeamDto(
    int      Id,
    int      TournamentId,
    string   Player1,
    string?  Player2,
    string   Name,
    int?     Seed,
    int?     StartMatchPos,
    DateTime RegistrationDate,
    string?  RegistrationCode
);

public record RegisterTeamsBulkDto(string TeamsText);

public static class TeamEndpoints
{
    #region Dto-Entity Mapping

    private static TeamDto? ToDto(Team? entity)
    {
        if (entity is null) return null;

        return new TeamDto(
            entity.Id,
            entity.TournamentId,
            entity.Player1,
            entity.Player2,
            entity.Name,
            entity.Seed,
            entity.StartMatchPos,
            entity.RegistrationDate,
            entity.RegistrationCode);
    }

    private static void ApplySeedOrStartMatchPos(Team entity, TeamDto dto)
    {
        if (dto.StartMatchPos.HasValue)
        {
            entity.StartMatchPos = dto.StartMatchPos;
            entity.Seed          = null;
        }
        else
        {
            entity.StartMatchPos = null;
            entity.Seed          = dto.Seed;
        }
    }

    #endregion

    public static void MapTeamEndpoints(this IEndpointRouteBuilder app, string baseRoute)
    {
        var routeRead = app
            .MapGroup($"{baseRoute}/{{tournamentId:int}}/teams")
            .WithTags("Teams")
            .RequireAuthorization(Settings.AdminOrUserPolicyName)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var routeAdmin = app
            .MapGroup($"{baseRoute}/{{tournamentId:int}}/teams")
            .WithTags("Teams")
            .RequireAuthorization(Settings.AdminPolicyName);

        routeRead.MapGet("", async (int tournamentId, IUnitOfWork uow) =>
            {
                var teams = await uow.Teams.GetByTournamentAsync(tournamentId);
                return Results.Ok(teams.Select(ToDto).ToList());
            })
            .WithName("GetTeams")
            .Produces<List<TeamDto>>(StatusCodes.Status200OK);

        routeRead.MapGet("/{id:int}", async (int tournamentId, int id, IUnitOfWork uow) =>
            {
                var entity = await uow.Teams.GetByIdAsync(id);
                if (entity is null || entity.TournamentId != tournamentId)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Team not found",
                        detail: $"No team found with ID {id} in tournament {tournamentId}");
                }

                return Results.Ok(ToDto(entity));
            })
            .WithName("GetTeam")
            .Produces<TeamDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeAdmin.MapPost("", async (int tournamentId, TeamDto dto, IUnitOfWork uow, IHubNotificationService hub) =>
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

                var teamName = dto.Player2 is null ? dto.Player1 : $"{dto.Player1}/{dto.Player2}";
                var entity   = await uow.Tournaments.RegisterTeamAsync(tournamentId, teamName);

                ApplySeedOrStartMatchPos(entity, dto);

                await trans.CommitTransactionAsync();
                var pin = (await uow.Tournaments.GetByIdAsync(tournamentId))?.RegistrationPin ?? 0;
                await hub.NotifyTournamentTeamUpdatedAsync(pin);

                int id = entity.Id;
                return Results.Created(
                    $"{baseRoute}/{tournamentId}/teams/{id}",
                    ToDto(await uow.Teams.GetByIdAsync(id)));
            })
            .WithValidation<TeamDto>()
            .WithName("AddTeam")
            .Produces<TeamDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeAdmin.MapPut("/{id:int}", async (int tournamentId, int id, TeamDto dto, IUnitOfWork uow, IHubNotificationService hub) =>
            {
                if (id != dto.Id)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid request",
                        detail: "The ID in the URL does not match the ID in the request body");
                }

                if (dto.TournamentId != tournamentId)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid request",
                        detail: "The TournamentId in the body does not match the URL");
                }

                using var trans = await uow.BeginTransactionAsync();

                var entity = await uow.Teams.GetByIdAsync(id);
                if (entity is null || entity.TournamentId != tournamentId)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid request",
                        detail: $"No team found with ID {id} in tournament {tournamentId}");
                }

                entity.Player1 = dto.Player1;
                entity.Player2 = dto.Player2;
                ApplySeedOrStartMatchPos(entity, dto);

                await trans.CommitTransactionAsync();
                var pin = (await uow.Tournaments.GetByIdAsync(tournamentId))?.RegistrationPin ?? 0;
                await hub.NotifyTournamentTeamUpdatedAsync(pin);

                return Results.NoContent();
            })
            .WithValidation<TeamDto>()
            .WithName("UpdateTeam")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeAdmin.MapDelete("/{id:int}", async (int tournamentId, int id, IUnitOfWork uow, IHubNotificationService hub) =>
            {
                var entity = await uow.Teams.GetByIdAsync(id);
                if (entity is null || entity.TournamentId != tournamentId)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Team not found",
                        detail: $"No team found with ID {id} in tournament {tournamentId}");
                }

                uow.Teams.Remove(entity);
                await uow.SaveChangesAsync();
                var pin = (await uow.Tournaments.GetByIdAsync(tournamentId))?.RegistrationPin ?? 0;
                await hub.NotifyTournamentTeamUpdatedAsync(pin);

                return Results.NoContent();
            })
            .WithName("DeleteTeam")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeAdmin.MapPost("/bulk", async (int tournamentId, RegisterTeamsBulkDto dto, IUnitOfWork uow, IHubNotificationService hub) =>
            {
                var entries = dto.TeamsText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(raw =>
                    {
                        var  parts         = raw.Split(';');
                        var  name          = parts[0].Trim();
                        int? seed          = parts.Length > 1 && int.TryParse(parts[1].Trim(), out var s1) ? s1 : null;
                        int? startMatchPos = parts.Length > 2 && int.TryParse(parts[2].Trim(), out var s2) ? s2 : null;

                        return (Name: name, Seed: seed, StartMatchPos: startMatchPos);
                    })
                    .ToList();

                try
                {
                    using var trans      = await uow.BeginTransactionAsync();
                    var       teams      = await uow.Tournaments.RegisterTeamsAsync(tournamentId, entries);
                    var       tournament = await uow.Tournaments.GetByIdAsync(tournamentId);
                    await trans.CommitTransactionAsync();
                    await hub.NotifyTournamentTeamUpdatedAsync(tournament?.RegistrationPin ?? 0);

                    return Results.Ok(teams
                        .Select(t => new TeamRegistrationResultDto(t.Name, tournament?.RegistrationPin ?? 0, t.RegistrationCode!))
                        .ToList());
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Bulk registration failed",
                        detail: ex.Message);
                }
            })
            .WithName("RegisterTeamsBulk")
            .Produces<List<TeamRegistrationResultDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
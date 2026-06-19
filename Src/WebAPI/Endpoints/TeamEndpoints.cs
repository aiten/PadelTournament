namespace WebAPI.Endpoints;

using Base.Persistence.Contracts;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Persistence.Model;

using Service;

using System;
using System.Collections.Generic;
using System.Linq;

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

    #endregion

    public static void MapTeamEndpoints(this IEndpointRouteBuilder app, string baseRoute)
    {
        var routeUser = app
            .MapGroup($"{baseRoute}/{{tournamentId:int}}/teams")
            .WithTags("Teams")
            .RequireAuthorization(Settings.AdminOrUserPolicyName)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var routeAdmin = app
            .MapGroup($"{baseRoute}/{{tournamentId:int}}/teams")
            .WithTags("Teams")
            .RequireAuthorization(Settings.AdminPolicyName)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routeUser.MapGet("", async (int tournamentId, ITournamentService tournamentService) =>
            {
                var tournament = await tournamentService.SingleTournamentAsync(tournamentId, nameof(Tournament.Teams));
                return Results.Ok(tournament.Teams.Select(ToDto).ToList());
            })
            .WithName("GetTeams")
            .Produces<List<TeamDto>>(StatusCodes.Status200OK);

        routeUser.MapGet("/{id:int}", async (int tournamentId, int id, ITeamService teamService) =>
            {
                var entity = await teamService.SingleTeamAsync(id);
                EndpointTools.CheckTournamentId(tournamentId, entity.TournamentId);

                return Results.Ok(ToDto(entity));
            })
            .WithName("GetTeam")
            .Produces<TeamDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeUser.MapPost("", async (int tournamentId, TeamDto dto, ITournamentService tournamentService, ITeamService teamService, ITransactionProvider transactionProvider) =>
            {
                EndpointTools.CheckIdMustBe0(dto.Id);
                EndpointTools.CheckTournamentId(tournamentId, dto.TournamentId);

                using var trans = await transactionProvider.BeginTransactionAsync();

                var teamName = string.IsNullOrEmpty(dto.Player2) ? dto.Player1 : $"{dto.Player1}/{dto.Player2}";
                var entity   = await tournamentService.RegisterTeamAsync(tournamentId, teamName, dto.Seed, dto.StartMatchPos);

                await trans.CommitTransactionAsync();

                int id = entity.Id;
                return Results.Created(
                    $"{baseRoute}/{tournamentId}/teams/{id}",
                    ToDto(await teamService.GetTeamByIdAsync(id)));
            })
            .WithValidation<TeamDto>()
            .WithName("AddTeam")
            .Produces<TeamDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeUser.MapPut("/{id:int}", async (int tournamentId, int id, TeamDto dto, ITeamService teamService, ITransactionProvider transactionProvider) =>
            {
                EndpointTools.CheckId(id, dto.Id);
                EndpointTools.CheckTournamentId(tournamentId, dto.TournamentId);

                using var trans = await transactionProvider.BeginTransactionAsync();

                await teamService.UpdateTeamAsync(id, tournamentId, dto.Player1, dto.Player2, dto.Seed, dto.StartMatchPos);

                await trans.CommitTransactionAsync();

                return Results.NoContent();
            })
            .WithValidation<TeamDto>()
            .WithName("UpdateTeam")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeUser.MapDelete("/{id:int}", async (int tournamentId, int id, ITeamService teamService, ITransactionProvider transactionProvider) =>
            {
                using var trans = await transactionProvider.BeginTransactionAsync();

                await teamService.DeleteTeamAsync(id, tournamentId);

                await trans.CommitTransactionAsync();

                return Results.NoContent();
            })
            .WithName("DeleteTeam")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeUser.MapPost("/bulk", async (int tournamentId, RegisterTeamsBulkDto dto, ITournamentService tournamentService, ITransactionProvider transactionProvider) =>
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

                using var trans      = await transactionProvider.BeginTransactionAsync();
                var       teams      = await tournamentService.RegisterTeamsAsync(tournamentId, entries);
                var       tournament = await tournamentService.SingleTournamentAsync(tournamentId);

                await trans.CommitTransactionAsync();

                return Results.Ok(teams
                    .Select(t => new TeamRegistrationResultDto(t.Name, tournament.RegistrationPin ?? 0, t.RegistrationCode!))
                    .ToList());
            })
            .WithName("RegisterTeamsBulk")
            .Produces<List<TeamRegistrationResultDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
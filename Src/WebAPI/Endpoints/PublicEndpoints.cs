namespace WebAPI.Endpoints;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Persistence;
using Persistence.Model;

public record PublicMatchResultDto(bool Won);

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app, string baseRoute)
    {
        var routeTeam = app
            .MapGroup($"{baseRoute}/{{pin:int}}/{{registrationCode}}")
            .WithTags("Public");
        // No authentication required

        var routeTournament = app
            .MapGroup($"{baseRoute}/{{pin:int}}")
            .WithTags("Public");

        routeTeam.MapGet("/team", async (int pin, string registrationCode, IUnitOfWork uow) =>
            {
                var team = await uow.Teams.GetByRegistrationAsync(pin, registrationCode);
                if (team is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Team not found",
                        detail: "No team found for the given pin and registration code");
                }

                return Results.Ok(new TeamDto(
                    team.Id,
                    team.TournamentId,
                    team.Player1,
                    team.Player2,
                    team.Name,
                    team.Seed,
                    team.StartMatchPos,
                    team.RegistrationDate,
                    team.RegistrationCode));
            })
            .WithName("GetPublicTeamInfo")
            .Produces<TeamDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeTeam.MapGet("/matches", async (int pin, string registrationCode, IUnitOfWork uow) =>
            {
                var team = await uow.Teams.GetByRegistrationAsync(pin, registrationCode);
                if (team is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Team not found",
                        detail: "No team found for the given pin and registration code");
                }

                var matches = await uow.Matches.GetByTeamAsync(team.Id);

                return Results.Ok(matches.Select(MatchEndpoints.ToDto).ToList());
            })
            .WithName("GetPublicTeamMatches")
            .Produces<List<MatchDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeTeam.MapPut("/matches/{matchId:int}/result", async (int pin, string registrationCode, int matchId, PublicMatchResultDto dto, IUnitOfWork uow) =>
            {
                var team = await uow.Teams.GetByRegistrationAsync(pin, registrationCode);
                if (team is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Team not found",
                        detail: "No team found for the given pin and registration code");
                }

                var match = await uow.Matches.GetByIdAsync(matchId);
                if (match is null || (match.TeamAId != team.Id && match.TeamBId != team.Id))
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Match not found",
                        detail: $"No match found with ID {matchId} for this team");
                }

                var isForA = match.TeamAId == team.Id;
                var winner = isForA
                    ? (dto.Won ? MatchResult.WonA : MatchResult.WonB)
                    : (dto.Won ? MatchResult.WonB : MatchResult.WonA);

                try
                {
                    using var trans = await uow.BeginTransactionAsync();
                    await uow.Matches.AcceptResultAsync(matchId, isForA, winner);
                    await trans.CommitTransactionAsync();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Cannot set result",
                        detail: ex.Message);
                }

                return Results.NoContent();
            })
            .WithName("ReportPublicMatchResult")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeTournament.MapGet("", async (int pin, IUnitOfWork uow) =>
            {
                var tournament = TournamentEndpoints.ToDto(await uow.Tournaments.GetByPinAsync(pin));

                if (tournament is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Tournament not found",
                        detail: $"No Tournament found with Pin {pin}");
                }

                return Results.Ok(tournament);
            })
            .WithName("GetPublicTournament")
            .Produces<TournamentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);


        routeTournament.MapGet("/teams", async (int pin, IUnitOfWork uow) =>
            {
                var tournament = await uow.Tournaments.GetByPinAsync(pin);
                if (tournament is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Tournament not found",
                        detail: "No tournament found for the given pin");
                }

                var teams = await uow.Teams.GetByTournamentAsync(tournament.Id);
                return Results.Ok(teams.Select(t => new TeamDto(
                    t.Id,
                    t.TournamentId,
                    t.Player1,
                    t.Player2,
                    t.Name,
                    null, // t.Seed,
                    null, // t.StartMatchPos,
                    t.RegistrationDate,
                    null)).ToList());
            })
            .WithName("GetPublicBracketTeams")
            .Produces<List<TeamDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeTournament.MapGet("/matches", async (int pin, IUnitOfWork uow) =>
            {
                var tournament = await uow.Tournaments.GetByPinAsync(pin);
                if (tournament is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Tournament not found",
                        detail: "No tournament found for the given pin");
                }

                var matches = await uow.Matches.GetByTournamentAsync(tournament.Id);
                return Results.Ok(matches.Select(MatchEndpoints.ToDto).ToList());
            })
            .WithName("GetPublicBracketMatches")
            .Produces<List<MatchDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
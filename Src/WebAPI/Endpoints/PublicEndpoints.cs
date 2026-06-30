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

using Shared.Exceptions;

public record PublicMatchResultDto(bool Won);

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app, string baseRoute)
    {
        var routeTeam = app
            .MapGroup($"{baseRoute}/{{pin}}/{{registrationCode}}")
            .WithTags("Public");
        // No authentication required

        var routeTournament = app
            .MapGroup($"{baseRoute}/{{pin}}")
            .WithTags("Public");

        routeTeam.MapGet("/team", async (string pin, string registrationCode, ITeamService teamService) =>
            {
                var team = await teamService.SingleByRegistrationAsync(pin, registrationCode);

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

        routeTeam.MapGet("/matches", async (string pin, string registrationCode, ITeamService teamService) =>
            {
                var team    = await teamService.SingleByRegistrationAsync(pin, registrationCode);
                var matches = await teamService.GetMatchesByTeamIdAsync(team.Id);

                return Results.Ok(matches.Select(MatchEndpoints.ToDto).ToList());
            })
            .WithName("GetPublicTeamMatches")
            .Produces<List<MatchDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeTeam.MapPut("/matches/{matchId:int}/result", async (string pin, string registrationCode, int matchId, PublicMatchResultDto dto, ITeamService teamService, IMatchService matchService, ITransactionProvider transactionProvider) =>
            {
                var team  = await teamService.SingleByRegistrationAsync(pin, registrationCode);
                var match = await matchService.SingleMatchForTeamAsync(matchId, team.Id);

                var isForA = match.TeamAId == team.Id;
                var winner = isForA
                    ? (dto.Won ? MatchResult.WonA : MatchResult.WonB)
                    : (dto.Won ? MatchResult.WonB : MatchResult.WonA);

                using var trans = await transactionProvider.BeginTransactionAsync();
                await matchService.AcceptResultAsync(matchId, isForA, winner);
                await trans.CommitTransactionAsync();

                return Results.NoContent();
            })
            .WithName("ReportPublicMatchResult")
            .RequireRateLimiting("public-lookup")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeTournament.MapGet("", async (string pin, ITournamentService tournamentService) =>
            {
                var tournament = TournamentEndpoints.ToDto(await tournamentService.SingleTournamentByPinAsync(pin));
                return Results.Ok(tournament);
            })
            .WithName("GetPublicTournament")
            .Produces<TournamentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);


        routeTournament.MapGet("/teams", async (string pin, ITournamentService tournamentService) =>
            {
                var tournament = await tournamentService.SingleTournamentByPinAsync(pin, loadTeams: true);

                var teams = tournament.Teams;
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

        routeTournament.MapGet("/matches", async (string pin, ITournamentService tournamentService) =>
            {
                var tournament = await tournamentService.SingleTournamentByPinAsync(pin, loadMatches: true);

                var matches = tournament.Matches;
                return Results.Ok(matches.Select(MatchEndpoints.ToDto).ToList());
            })
            .WithName("GetPublicBracketMatches")
            .Produces<List<MatchDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
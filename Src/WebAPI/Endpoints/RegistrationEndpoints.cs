namespace WebAPI.Endpoints;

using System;

using Core.Contracts;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using WebAPI.Filters;

public record TeamRegistrationDto(string Name, int Pin);

public record TeamRegistrationResultDto(string Name, int Pin, string RegistrationCode);

public static class RegistrationEndpoints
{
    public static void MapRegistrationEndpoints(this IEndpointRouteBuilder app, string baseRoute)
    {
        var route = app
            .MapGroup(baseRoute)
            .WithTags("Registration");
        // NO Auth required .RequireAuthorization(Settings.AdminPolicyName);

        route.MapPost("", async (TeamRegistrationDto dto, IUnitOfWork uow) =>
            {
                try
                {
                    using var trans        = await uow.BeginTransactionAsync();
                    var       registration = await uow.Tournaments.RegisterTeamByPinAsync(dto.Name, dto.Pin);

                    await trans.CommitTransactionAsync();

                    return Results.Created($"/api/public/{dto.Pin}/{registration.RegistrationCode}",
                        new TeamRegistrationResultDto(
                            registration.Name,
                            dto.Pin,
                            registration.RegistrationCode!));
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Registration failed",
                        detail: ex.Message);
                }
            })
            .WithValidation<TeamRegistrationDto>()
            .WithName("RegisterForTournament")
            .Produces<TeamRegistrationResultDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
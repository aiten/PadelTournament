namespace WebAPI.Endpoints;

using System.Collections.Generic;
using System.Linq;

using Base.Persistence.Contracts;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Persistence.Model;

using Service;

using WebAPI.Filters;

public record FormatDto(
    int     Id,
    string  Name,
    string  PlayingFormat,
    int?    BestOf        = 3,
    int?    GamesToWinSet = 6,
    int?    MinMargin       = 2,
    bool    NoAdv         = false,
    bool    NoTiebreak    = false
);

public static class FormatEndpoints
{
    #region Dto-Entity Mapping

    private static PlayingFormat? EntityPlayingFormat(string? playingFormat)
    {
        return playingFormat switch
        {
            "Tennis" => PlayingFormat.Tennis,
            "Padel"  => PlayingFormat.Padel,
            "Soccer" => PlayingFormat.Soccer,
            _        => null
        };
    }

    private static Format ToEntity(FormatDto dto)
    {
        return new Format()
        {
            Id            = dto.Id,
            Name          = dto.Name,
            PlayingFormat = EntityPlayingFormat(dto.PlayingFormat) ?? default,
            BestOf        = dto.BestOf,
            GamesToWinSet = dto.GamesToWinSet,
            MinMargin       = dto.MinMargin,
            NoAdv         = dto.NoAdv,
            NoTiebreak    = dto.NoTiebreak
        };
    }

    private static FormatDto? ToDto(Format? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new FormatDto(
            entity.Id,
            entity.Name,
            entity.PlayingFormat.ToString(),
            entity.BestOf,
            entity.GamesToWinSet,
            entity.MinMargin,
            entity.NoAdv,
            entity.NoTiebreak
        );
    }

    #endregion

    public static void MapFormatEndpoints(this IEndpointRouteBuilder app, string baseRoute)
    {
        var routeUser = app
            .MapGroup(baseRoute)
            .WithTags("Format")
            .RequireAuthorization(Settings.AdminOrUserPolicyName)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        var routeAdmin = app
            .MapGroup(baseRoute)
            .WithTags("Format")
            .RequireAuthorization(Settings.AdminPolicyName)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        routeUser.MapGet("", async (IFormatService formatService) =>
            {
                var dtos = (await formatService.GetFormatsAsync()).Select(ToDto).ToList();
                return Results.Ok(dtos);
            })
            .WithName("GetFormats")
            .Produces<List<FormatDto>>(StatusCodes.Status200OK);

        routeUser.MapGet("/{id:int}", async (int id, IFormatService formatService) =>
            {
                var dto = ToDto(await formatService.SingleFormatAsync(id));
                return Results.Ok(dto);
            })
            .WithName("GetFormat")
            .Produces<FormatDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeAdmin.MapPost("", async (FormatDto dto, IFormatService formatService, ITransactionProvider transactionProvider) =>
            {
                EndpointTools.CheckIdMustBe0(dto.Id);

                using var trans = await transactionProvider.BeginTransactionAsync();

                var created = await formatService.AddFormatAsync(ToEntity(dto));
                await trans.CommitTransactionAsync();

                return Results.Created($"{baseRoute}/{created.Id}", ToDto(created));
            })
            .WithValidation<FormatDto>()
            .WithName("AddFormat")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        routeAdmin.MapPut("/{id:int}", async (int id, FormatDto dto, IFormatService formatService, ITransactionProvider transactionProvider) =>
            {
                EndpointTools.CheckId(id, dto.Id);

                using var trans = await transactionProvider.BeginTransactionAsync();

                await formatService.UpdateFormatAsync(id, ToEntity(dto));
                await trans.CommitTransactionAsync();

                return Results.NoContent();
            })
            .WithValidation<FormatDto>()
            .WithName("UpdateFormat")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        routeAdmin.MapDelete("/{id:int}", async (int id, IFormatService formatService, ITransactionProvider transactionProvider) =>
            {
                using var trans = await transactionProvider.BeginTransactionAsync();

                await formatService.DeleteFormatAsync(id);
                await trans.CommitTransactionAsync();

                return Results.NoContent();
            })
            .WithName("DeleteFormat")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

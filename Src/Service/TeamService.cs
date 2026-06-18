namespace Service;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Persistence;
using Persistence.Model;
using Persistence.QueryResult;

using Shared.Exceptions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

public interface ITeamService
{
    Task<Team?> GetByIdAsync(int id, params string[] includeProperties);
    Task<Team>  SingleAsync(int  id, params string[] includeProperties);

    Task UpdateAsync(int id, int tournamentId, string player1, string? player2, int? seed, int? startMatchPos);

    Task DeleteTeamAsync(int id, int tournamentId);

}

public class TeamService : ITeamService
{
    private readonly IUnitOfWork             _uow;
    private readonly ILogger<TeamService>    _logger;
    private readonly IHubNotificationService _hub;

    public TeamService(IUnitOfWork uow, ILogger<TeamService> logger, IHubNotificationService hub)
    {
        _uow    = uow;
        _logger = logger;
        _hub    = hub;
    }

    #region REST

    public async Task<Team?> GetByIdAsync(int id, params string[] includeProperties)
    {
        var entity = await _uow.Teams.GetByIdAsync(id, includeProperties);
        return entity;
    }

    public async Task<Team> SingleAsync(int id, params string[] includeProperties)
    {
        return (await GetByIdAsync(id, includeProperties)) ?? throw new NotFoundException($"Team {id} not found");
    }

    private async Task<Team> CheckTeamAsync(int id, int tournamentId, params string[] includeProperties)
    {
        var entity = await SingleAsync(id, includeProperties);
        if (entity.TournamentId != tournamentId)
        {
            throw new NotFoundException($"Team {id} not found in tournament {tournamentId}");
        }
        return entity;
    }

    public async Task UpdateAsync(int id, int tournamentId, string player1, string? player2, int? seed, int? startMatchPos)
    {
        var entity = await CheckTeamAsync(id, tournamentId, nameof(Team.Tournament));

        entity.Player1 = player1;
        entity.Player2 = player2;
        ApplySeedOrStartMatchPos(entity, seed, startMatchPos);

        await _uow.SaveChangesAsync();
        await _hub.NotifyTournamentTeamUpdatedAsync(entity.Tournament.RegistrationPin??0);
    }

    public async Task DeleteTeamAsync(int id, int tournamentId)
    {
        var entity = await CheckTeamAsync(id, tournamentId, nameof(Team.Tournament));
        _uow.Teams.Remove(entity);

        await _uow.SaveChangesAsync();
        await _hub.NotifyTournamentTeamUpdatedAsync(entity.Tournament.RegistrationPin ?? 0);
    }

    private static void ApplySeedOrStartMatchPos(Team entity, int? seed, int? startMatchPos)
    {
        if (startMatchPos.HasValue)
        {
            entity.StartMatchPos = startMatchPos;
            entity.Seed          = null;
        }
        else
        {
            entity.StartMatchPos = null;
            entity.Seed          = seed;
        }
    }

    #endregion
}
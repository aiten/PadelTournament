namespace Service;

using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Persistence;
using Persistence.Model;

using Shared.Exceptions;

using System.Threading.Tasks;

public interface ITeamService
{
    Task<Team?> GetTeamByIdAsync(int id, params string[] includeProperties);
    Task<Team>  SingleTeamAsync(int  id, params string[] includeProperties);

    Task UpdateTeamAsync(int id, int tournamentId, string player1, string? player2, int? seed, int? startMatchPos);

    Task DeleteTeamAsync(int id, int tournamentId);

    Task<Team> SingleByRegistrationAsync(int pin, string registrationCode);

    Task<IList<Match>> GetMatchesByTeamIdAsync(int teamId);
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

    public async Task<Team?> GetTeamByIdAsync(int id, params string[] includeProperties)
    {
        var entity = await _uow.Teams.GetByIdAsync(id, includeProperties);
        return entity;
    }

    public async Task<Team> SingleTeamAsync(int id, params string[] includeProperties)
    {
        return (await GetTeamByIdAsync(id, includeProperties)) ?? throw new NotFoundException($"Team {id} not found");
    }

    private async Task<Team> CheckTeamAsync(int id, int tournamentId, params string[] includeProperties)
    {
        var entity = await SingleTeamAsync(id, includeProperties);
        if (entity.TournamentId != tournamentId)
        {
            throw new NotFoundException($"Team {id} not found in tournament {tournamentId}");
        }

        return entity;
    }

    public async Task UpdateTeamAsync(int id, int tournamentId, string player1, string? player2, int? seed, int? startMatchPos)
    {
        var entity = await CheckTeamAsync(id, tournamentId, nameof(Team.Tournament));

        entity.Player1 = player1;
        entity.Player2 = string.IsNullOrEmpty(player2) ? null : player2;
        ApplySeedOrStartMatchPos(entity, seed, startMatchPos);

        await _uow.SaveChangesAsync();
        await _hub.NotifyTournamentTeamUpdatedAsync(entity.Tournament.RegistrationPin ?? 0);
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

    public async Task<Team> SingleByRegistrationAsync(int pin, string registrationCode)
    {
        var team = await _uow.Teams.GetByRegistrationAsync(pin, registrationCode);
        return team ?? throw new NotFoundException("No team found for the given pin and registration code");
    }

    public async Task<IList<Match>> GetMatchesByTeamIdAsync(int teamId)
    {
        return await _uow.Matches.GetByTeamAsync(teamId);
    }
}
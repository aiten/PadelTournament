using Microsoft.AspNetCore.SignalR;

using Service;

using WebAPI.Hubs;

namespace WebAPI.Services;

using System.Threading.Tasks;

public class HubNotificationService(
    IHubContext<TournamentHub, ITournamentHubClient> hubContext,
    IHubContext<PublicHub, IPublicHubClient>         publicHubContext) : IHubNotificationService
{
    public Task NotifyTournamentUpdatedAsync(int tournamentId) =>
        hubContext.Clients.Group($"tournament-{tournamentId}").TournamentUpdated(tournamentId);

    public Task NotifyTournamentMatchUpdatedAsync(string pin, int? matchId = null) =>
        publicHubContext.Clients.Group($"pin-{pin}").TournamentMatchUpdated(pin, matchId);

    public Task NotifyTournamentTeamUpdatedAsync(string pin) =>
        publicHubContext.Clients.Group($"pin-{pin}").TournamentTeamUpdated(pin);
}
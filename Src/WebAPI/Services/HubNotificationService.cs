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
        hubContext.Clients.All.TournamentUpdated(tournamentId);

    public Task NotifyTournamentMatchUpdatedAsync(string pin) =>
        publicHubContext.Clients.Group($"pin-{pin}").TournamentMatchUpdated(pin);

    public Task NotifyTournamentTeamUpdatedAsync(string pin) =>
        publicHubContext.Clients.Group($"pin-{pin}").TournamentTeamUpdated(pin);
}
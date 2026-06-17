using Microsoft.AspNetCore.SignalR;

using Service;

using WebAPI.Hubs;

namespace WebAPI.Services;

using System.Threading.Tasks;

public class HubNotificationService(IHubContext<TournamentHub, ITournamentHubClient> hubContext) : IHubNotificationService
{
    public Task NotifyTournamentUpdatedAsync(int tournamentId) =>
        hubContext.Clients.All.TournamentUpdated(tournamentId);
}

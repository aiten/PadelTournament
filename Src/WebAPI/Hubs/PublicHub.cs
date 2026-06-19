using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebAPI.Hubs;

using System.Threading.Tasks;

[AllowAnonymous]
public class PublicHub : Hub<IPublicHubClient>
{
    public async Task JoinTournamentGroup(string pin) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"pin-{pin}");

    public async Task LeaveTournamentGroup(string pin) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"pin-{pin}");
}
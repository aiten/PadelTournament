using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebAPI.Hubs;

using System.Threading.Tasks;

[Authorize(Policy = Settings.AdminOrUserPolicyName)]
public class TournamentHub : Hub<ITournamentHubClient>
{
    public async Task JoinTournamentGroup(int tournamentId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tournament-{tournamentId}");

    public async Task LeaveTournamentGroup(int tournamentId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tournament-{tournamentId}");
}

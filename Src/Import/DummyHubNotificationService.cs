namespace Import;

using System.Threading.Tasks;

using Service;

public class DummyHubNotificationService : IHubNotificationService
{
    public async Task NotifyTournamentUpdatedAsync(int tournamentId) => await Task.CompletedTask;

    public async Task NotifyTournamentMatchUpdatedAsync(string pin, int? matchId = null) => await Task.CompletedTask;

    public async Task NotifyTournamentTeamUpdatedAsync(string pin) => await Task.CompletedTask;
}
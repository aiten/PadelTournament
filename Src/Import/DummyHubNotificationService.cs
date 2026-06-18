namespace Import;

using System.Threading.Tasks;

using Service;

public class DummyHubNotificationService : IHubNotificationService
{
    public async Task NotifyTournamentUpdatedAsync(int tournamentId) => await Task.CompletedTask;

    public async Task NotifyTournamentMatchUpdatedAsync(int pin) => await Task.CompletedTask;

    public async Task NotifyTournamentTeamUpdatedAsync(int pin) => await Task.CompletedTask;
}
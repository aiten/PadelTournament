using System.Threading.Tasks;

namespace Service;

public interface IHubNotificationService
{
    Task NotifyTournamentUpdatedAsync(int tournamentId);
    Task NotifyTournamentMatchUpdatedAsync(int pin);
    Task NotifyTournamentTeamUpdatedAsync(int pin);
}

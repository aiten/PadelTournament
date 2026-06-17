using System.Threading.Tasks;

namespace Service;

public interface IHubNotificationService
{
    Task NotifyTournamentUpdatedAsync(int tournamentId);
    Task NotifyTournamentMatchUpdatedAsync(int tournamentId);
    Task NotifyTournamentTeamUpdatedAsync(int tournamentId);
}

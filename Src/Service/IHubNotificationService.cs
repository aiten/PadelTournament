using System.Threading.Tasks;

namespace Service;

public interface IHubNotificationService
{
    Task NotifyTournamentUpdatedAsync(int      tournamentId);
    Task NotifyTournamentMatchUpdatedAsync(string pin);
    Task NotifyTournamentTeamUpdatedAsync(string  pin);
}
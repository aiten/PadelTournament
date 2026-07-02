using System.Threading.Tasks;

namespace Service;

public interface IHubNotificationService
{
    Task NotifyTournamentUpdatedAsync(int      tournamentId);
    Task NotifyTournamentMatchUpdatedAsync(string pin, int? matchId = null);
    Task NotifyTournamentTeamUpdatedAsync(string  pin);
}
namespace WebAPI.Hubs;

using System.Threading.Tasks;

public interface IPublicHubClient
{
    Task TournamentMatchUpdated(int pin);
    Task TournamentTeamUpdated(int pin);
}

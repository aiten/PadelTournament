namespace WebAPI.Hubs;

using System.Threading.Tasks;

public interface IPublicHubClient
{
    Task TournamentMatchUpdated(string pin);
    Task TournamentTeamUpdated(string  pin);
}
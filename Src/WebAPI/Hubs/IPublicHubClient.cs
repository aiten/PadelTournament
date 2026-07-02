namespace WebAPI.Hubs;

using System.Threading.Tasks;

public interface IPublicHubClient
{
    Task TournamentMatchUpdated(string pin, int? matchId);
    Task TournamentTeamUpdated(string  pin);
}
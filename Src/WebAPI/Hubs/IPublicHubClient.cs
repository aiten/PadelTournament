namespace WebAPI.Hubs;

using System.Threading.Tasks;

public interface IPublicHubClient
{
    Task TournamentMatchUpdated(int tournamentId);
    Task TournamentTeamUpdated(int tournamentId);
}

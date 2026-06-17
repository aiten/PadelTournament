namespace WebAPI.Hubs;

using System.Threading.Tasks;

public interface ITournamentHubClient
{
    Task TournamentUpdated(int tournamentId);
}

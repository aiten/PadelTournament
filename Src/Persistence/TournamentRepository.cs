namespace Persistence;

using Base.Persistence;
using Base.Persistence.Contracts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Persistence.Model;
using Persistence.QueryResult;

public interface ITournamentRepository : IGenericRepository<Tournament>
{
    Task<IList<TournamentOverview>> GetTournamentOverviewsAsync();

    Task<Tournament?> GetByPinAsync(int pin);

    Task<Team> RegisterTeamAsync(int tournamentId, string name);

    Task<Team> RegisterTeamByPinAsync(string name, int pin);

    Task<IList<Team>> RegisterTeamsAsync(int tournamentId, IList<(string Name, int? Seed, int? StartMatchPos)> teams);
}

public class TournamentRepository : GenericRepository<Tournament>, ITournamentRepository
{
    private readonly ApplicationDbContext          _dbContext;
    private readonly ILogger<TournamentRepository> _logger;

    public TournamentRepository(ApplicationDbContext dbContext, ILogger<TournamentRepository> logger) : base(dbContext)
    {
        _dbContext = dbContext;
        _logger    = logger;
    }

    public async Task<IList<TournamentOverview>> GetTournamentOverviewsAsync()
    {
        return await _dbContext.Tournaments
            .Select(t => new TournamentOverview(
                t.Id,
                t.Description,
                t.RegistrationPin,
                t.From,
                t.To,
                t.Teams.Count,
                t.Matches.Count(),
                t.Matches.Count(m => m.Result != null)
            ))
            .ToListAsync();
    }

    public async Task<Tournament?> GetByPinAsync(int pin)
    {
        return await _dbContext.Tournaments.FirstOrDefaultAsync(t => t.RegistrationPin == pin);
    }

    public async Task<Team> RegisterTeamAsync(int tournamentId, string name)
    {
        var tournament = await GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new InvalidOperationException($"No tournament found with Id {tournamentId}");

        return await RegisterTeamAsync(tournament, name);
    }

    public async Task<Team> RegisterTeamByPinAsync(string name, int pin)
    {
        var tournament = await _dbContext.Tournaments.FirstOrDefaultAsync(e => e.RegistrationPin == pin);
        if (tournament is null)
            throw new InvalidOperationException($"No tournament found with PIN {pin}");

        return await RegisterTeamAsync(tournament, name);
    }

    private async Task<Team> RegisterTeamAsync(Tournament tournament, string name)
    {
        bool alreadyStarted = await _dbContext.Matches
            .AnyAsync(se => se.TournamentId == tournament.Id);
        if (alreadyStarted)
            throw new InvalidOperationException($"Tournament '{name}' is already started");

        var slashIdx = name.IndexOf('/');
        var player1  = slashIdx >= 0 ? name[..slashIdx].Trim() : name.Trim();
        var p2Raw    = slashIdx >= 0 ? name[(slashIdx + 1)..].Trim() : null;
        var player2  = p2Raw is { Length: > 0 } ? p2Raw : null;

        bool alreadyRegistered = await _dbContext.Teams
            .AnyAsync(se => se.Player1 == player1 && se.Player2 == player2 && se.TournamentId == tournament.Id);
        if (alreadyRegistered)
            throw new InvalidOperationException($"Team '{name}' is already registered for this tournament");

        var registration = new Team
        {
            Player1          = player1,
            Player2          = player2,
            RegistrationCode = await GenerateUniqueRegistrationCodeAsync(tournament.Id),
            RegistrationDate = DateTime.Now,
            Tournament       = tournament
        };

        await _dbContext.Teams.AddAsync(registration);
        return registration;
    }

    public async Task<IList<Team>> RegisterTeamsAsync(int tournamentId, IList<(string Name, int? Seed, int? StartMatchPos)> teams)
    {
        var tournament = await GetByIdAsync(tournamentId);
        if (tournament is null)
            throw new InvalidOperationException($"No tournament found with Id {tournamentId}");

        var result = new List<Team>();
        foreach (var (name, seed, startMatchPos) in teams)
        {
            var team = await RegisterTeamAsync(tournament, name);
            team.Seed = seed;
            team.StartMatchPos = startMatchPos; 
            result.Add(team);
        }

        return result;
    }

    private async Task<string> GenerateUniqueRegistrationCodeAsync(int tournamentId)
    {
        var    rng = Random.Shared;
        string code;
        do
        {
            code = rng.Next(10000, 100000).ToString();
        } while (await _dbContext.Teams.AnyAsync(se => se.TournamentId == tournamentId && se.RegistrationCode == code));

        return code;
    }
}
using Core.Contracts;
using Core.Entities;

namespace Persistence;

using Base.Persistence;

using Core.QueryResult;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    public async Task<Tournament> GenerateMatchSchedule(int tournamentId)
    {
        var tournament = await GetByIdAsync(tournamentId, nameof(Tournament.Teams));
        if (tournament is null)
        {
            throw new InvalidOperationException($"No tournament found with ID {tournamentId}");
        }

        bool hasMatches = await _dbContext.Matches.AnyAsync(m => m.TournamentId == tournamentId);
        if (hasMatches)
            throw new InvalidOperationException("CreateMatches already exist for this tournament");

        if (tournament.Teams.Count < 2)
            throw new InvalidOperationException("At least 2 teams must be registered before generating a schedule");

        int rounds = (int)Math.Ceiling(Math.Log2(tournament.Teams.Count));

        CreateMatches(rounds, tournament);

        AssignTeamsToMatches(tournament);

        SetBye(tournament);

        return tournament;
    }

    private static List<Match> CreateMatches(int rounds, Tournament tournament)
    {
        var noInRound = new int[rounds];
        var matches   = new List<Match>();


        void CreateMatch(int round, Match? nextMatch)
        {
            if (round < 1)
            {
                return;
            }

            var match = new Match()
            {
                NextMatch  = nextMatch,
                Round      = round,
                No         = ++noInRound[round - 1],
                Tournament = tournament
            };

            matches.Add(match);

            CreateMatch(round - 1, match);
            CreateMatch(round - 1, match);
        }

        CreateMatch(rounds, null);

        matches = matches.OrderBy(m => (m.Round, m.No)).ToList();

        tournament.Matches = matches;
        return matches;
    }

    private static Dictionary<int, Team> CreateMatchOrder(Tournament tournament)
    {
        int count = tournament.Matches.Count;

        var noShuffle = Enumerable.Range(1, count / 2 + 1).ToArray().Shuffle().ToArray(); // 1..count/2+1

        var teams = new Dictionary<int, Team>();    // 0..count-1 => team
        var seeds = GenerateSeeds(count).ToArray(); // 1..count

        foreach (var team in tournament.Teams.Where(t => t.StartMatchPos is not null).OrderBy(t => t.StartMatchPos))
        {
            int matchPos = team.StartMatchPos!.Value;
            if (matchPos == 0 || Math.Abs(matchPos) > count)
            {
                throw new InvalidOperationException($"The start-match-pos {matchPos} is illegal at Team: {team.Name}");
            }
            if (matchPos < 0)
            {
                matchPos = count + matchPos + 2;
            }
            matchPos--;
            if (teams.ContainsKey(matchPos))
            {
                throw new InvalidOperationException($"The start-match-pos is specified twice Team: {team.Name} / {teams[matchPos].Name}");
            }
            teams[matchPos] = team;
        }

        int idx = 0;
        foreach (var team in tournament.Teams.Where(t => t.StartMatchPos is null && t.Seed is not null).OrderBy(t => t.Seed))
        {
            int matchPos = seeds[idx++] - 1;
            if (teams.ContainsKey(matchPos))
            {
                throw new InvalidOperationException($"The seed position is occupied by a start-match-pos Team: {team.Name} / {teams[matchPos].Name}");
            }
            teams[matchPos] = team;
        }

        foreach (var team in tournament.Teams.Where(t => t.StartMatchPos is null && t.Seed is null))
        {
            // first find empty match
            int gameIdx = noShuffle.FirstOrDefault(no => !teams.ContainsKey(no * 2 - 2) && !teams.ContainsKey(no * 2 - 1)) * 2 - 2;
            if (gameIdx < 0) // 0 is default value but *2 -2 , means no match found
            {
                gameIdx = noShuffle.FirstOrDefault(no => !teams.ContainsKey(no * 2 - 2)) * 2 - 2;

                if (gameIdx < 0)
                {
                    gameIdx = noShuffle.FirstOrDefault(no => !teams.ContainsKey(no * 2 - 1)) * 2 - 1;
                }
            }

            teams[gameIdx] = team;
        }

        return teams;
    }

    private static void AssignTeamsToMatches(Tournament tournament)
    {
        var matchOrder = CreateMatchOrder(tournament);

        var firstRoundMatches = tournament.Matches.Where(m => m.Round == 1).OrderBy(m => m.No).ToList();

        foreach (var match in firstRoundMatches)
        {
            if (matchOrder.TryGetValue(match.No * 2 - 2, out var teamA))
            {
                match.TeamA = teamA;
            }

            if (matchOrder.TryGetValue(match.No * 2 - 1, out var teamB))
            {
                match.TeamB = teamB;
            }
        }
    }

    private static List<int> GenerateSeeds(int count)
    {
        // S(1) = 1
        // S(n)=[S(n−1), 2n+1−S(n−1)]

        List<int> MyGenerateSeeds(int n)
        {
            // n must be a power of 2
            var seeds = new List<int> { 1, 2 };

            while (seeds.Count < n)
            {
                int mirror = seeds.Count * 2 + 1;
                var next   = new List<int>();

                foreach (int x in seeds)
                {
                    next.Add(x);
                    next.Add(mirror - x);
                }

                seeds = next;
            }

            return seeds;
        }

        return MyGenerateSeeds(count);
    }

    private static void SetBye(Tournament tournament)
    {
        var firstRoundMatchesBye = tournament.Matches.Where(m => m.Round == 1 && (m.TeamA == null || m.TeamB == null));

        foreach (var match in firstRoundMatchesBye)
        {
            Team team;
            if (match.TeamA == null)
            {
                team         = match.TeamB!;
                match.Result = MatchResult.WonB;
            }
            else
            {
                team         = match.TeamA!;
                match.Result = MatchResult.WonA;
            }

            match.Remark = "Bye";

            if (match.NextMatch is not null)
            {
                if ((match.No % 2) == 1)
                {
                    match.NextMatch.TeamA = team;
                }
                else
                {
                    match.NextMatch.TeamB = team;
                }
            }
        }
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

    public async Task DeleteMatchScheduleAsync(int tournamentId)
    {
        var entity = await GetByIdAsync(tournamentId, nameof(Tournament.Matches));

        if (entity is null)
        {
            throw new InvalidOperationException($"No tournament found with Id {tournamentId}");
        }

        _dbContext.Matches.RemoveRange(entity.Matches);
    }

    public async Task DeleteCascadeAsync(int tournamentId)
    {
        var entity = await GetByIdAsync(tournamentId, nameof(Tournament.Teams), nameof(Tournament.Matches));

        if (entity is null)
        {
            throw new InvalidOperationException($"No tournament found with Id {tournamentId}");
        }

        _dbContext.Teams.RemoveRange(entity.Teams);
        _dbContext.Matches.RemoveRange(entity.Matches);

        Remove(entity);
    }
}
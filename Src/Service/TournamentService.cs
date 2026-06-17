namespace Service;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Persistence;
using Persistence.Model;
using Persistence.QueryResult;

using Shared.Exceptions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public interface ITournamentService
{
    Task<IList<TournamentOverview>> GetTournamentOverviewsAsync();

    Task<Tournament> GetByIdAsync(int id, params string[] includeProperties);

    Task UpdateTournamentAsync(int id, Tournament tournament);

    Task<Tournament> AddTournamentAsync(Tournament Tournament);

    Task DeleteTournamentAsync(int id);

    Task<Tournament> GenerateMatchScheduleAsync(int tournamentId);

    Task DeleteMatchScheduleAsync(int tournamentId);
}

public class TournamentService : ITournamentService
{
    private readonly IUnitOfWork                _uow;
    private readonly ILogger<TournamentService> _logger;
    private readonly IHubNotificationService    _hub;

    public TournamentService(IUnitOfWork uow, ILogger<TournamentService> logger, IHubNotificationService hub)
    {
        _uow    = uow;
        _logger = logger;
        _hub    = hub;
    }

    #region REST

    public async Task<IList<TournamentOverview>> GetTournamentOverviewsAsync()
    {
        return await _uow.Tournaments.GetTournamentOverviewsAsync();
    }

    public async Task<Tournament> GetByIdAsync(int id, params string[] includeProperties)
    {
        var entity = await _uow.Tournaments.GetByIdAsync(id, includeProperties);
        return entity ?? throw new NotFoundException($"Tournament {id} not found");
    }

    public async Task UpdateTournamentAsync(int id, Tournament tournament)
    {
        var entity = await _uow.Tournaments.GetByIdAsync(id);

        if (entity == null)
        {
            throw new NotFoundException($"Tournament {id} not found");
        }

        entity.Description     = tournament.Description;
        entity.RegistrationPin = tournament.RegistrationPin;
        entity.From            = tournament.From;
        entity.To              = tournament.To;
        entity.Modified        = DateTime.Now;

        await _uow.SaveChangesAsync();
        await _hub.NotifyTournamentUpdatedAsync(id);
    }

    public async Task<Tournament> AddTournamentAsync(Tournament Tournament)
    {
        if (Tournament.Id != 0)
        {
            throw new IllegalValuesException("Id must be 0 for new entities");
        }

        Tournament.Created  = DateTime.Now;
        Tournament.Modified = null;

        await _uow.Tournaments.AddAsync(Tournament);
        await _uow.SaveChangesAsync();
        await _hub.NotifyTournamentUpdatedAsync(Tournament.Id);

        return Tournament;
    }

    public async Task DeleteTournamentAsync(int id)
    {
        var entity = await _uow.Tournaments.GetByIdAsync(id, nameof(Tournament.Teams), nameof(Tournament.Matches));

        if (entity == null)
        {
            throw new NotFoundException($"Tournament {id} not found");
        }

        _uow.Teams.RemoveRange(entity.Teams);
        _uow.Matches.RemoveRange(entity.Matches);

        _uow.Tournaments.Remove(entity);
        await _uow.SaveChangesAsync();
        await _hub.NotifyTournamentUpdatedAsync(id);
    }

    #endregion

    #region Genrate Schedule

    public async Task<Tournament> GenerateMatchScheduleAsync(int tournamentId)
    {
        var tournament = await _uow.Tournaments.GetByIdAsync(tournamentId, nameof(Tournament.Teams));
        if (tournament is null)
        {
            throw new NotFoundException($"No tournament found with ID {tournamentId}");
        }

        bool hasMatches = await _uow.Matches.AnyTournamentAsync(tournamentId);
        if (hasMatches)
            throw new InvalidTournamentDataException("Matches already exist for this tournament");

        if (tournament.Teams.Count < 2)
            throw new InvalidTournamentDataException("At least 2 teams must be registered before generating a schedule");

        int rounds = (int)Math.Ceiling(Math.Log2(tournament.Teams.Count));

        CreateMatches(rounds, tournament);

        AssignTeamsToMatches(tournament);

        SetBye(tournament);

        await _uow.SaveChangesAsync();

        await _hub.NotifyTournamentUpdatedAsync(tournament.Id);
        await _hub.NotifyTournamentMatchUpdatedAsync(tournament.RegistrationPin??1);

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
                throw new InvalidTournamentDataException($"The start-match-pos {matchPos} is illegal at Team: {team.Name}");
            }

            if (matchPos < 0)
            {
                matchPos = count + matchPos + 2;
            }

            matchPos--;
            if (teams.ContainsKey(matchPos))
            {
                throw new InvalidTournamentDataException($"The start-match-pos is specified twice Team: {team.Name} / {teams[matchPos].Name}");
            }

            teams[matchPos] = team;
        }

        int idx = 0;
        foreach (var team in tournament.Teams.Where(t => t.StartMatchPos is null && t.Seed is not null).OrderBy(t => t.Seed))
        {
            int matchPos = seeds[idx++] - 1;
            if (teams.ContainsKey(matchPos))
            {
                throw new InvalidTournamentDataException($"The seed position is occupied by a start-match-pos Team: {team.Name} / {teams[matchPos].Name}");
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

    #endregion

    #region Schedule Helper

    public async Task DeleteMatchScheduleAsync(int tournamentId)
    {
        var entity = await GetByIdAsync(tournamentId, nameof(Tournament.Matches));

        if (entity is null)
        {
            throw new NotFoundException($"No tournament found with Id {tournamentId}");
        }

        _uow.Matches.RemoveRange(entity.Matches);
    }

    #endregion
}
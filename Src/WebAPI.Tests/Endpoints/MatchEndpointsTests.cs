using System;
using System.Net;
using System.Net.Http.Json;

using Core.Entities;

using FluentAssertions;

using NSubstitute;

using WebAPI;
using WebAPI.Endpoints;

namespace WebAPI.Tests.Endpoints;

public class MatchEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    private const int TournamentId = 1;

    public MatchEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Reset();
        _client = factory.CreateClient();
    }

    private void AsAdmin()           => _factory.TestAuth.Roles = [Settings.KeycloakAdminRoleName];
    private void AsUser()            => _factory.TestAuth.Roles = [Settings.KeycloakUserRoleName];
    private void AsUnauthenticated() => _factory.TestAuth.IsAuthenticated = false;

    private static string MatchesUrl(int tournamentId = TournamentId) =>
        $"/api/tournament/{tournamentId}/matches";

    private static string MatchUrl(int id, int tournamentId = TournamentId) =>
        $"/api/tournament/{tournamentId}/matches/{id}";

    private static Match MakeMatch(int id, int tournamentId = TournamentId) => new()
    {
        Id           = id,
        TournamentId = tournamentId,
        Round        = 1,
        No           = id,
        Result       = 0
    };

    // ─── GET /api/tournament/{id}/matches ────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList_WhenAuthenticated()
    {
        AsUser();
        var matches = new List<Match> { MakeMatch(1), MakeMatch(2) };
        _factory.MatchRepository.GetByTournamentAsync(TournamentId).Returns(matches);

        var response = await _client.GetAsync(MatchesUrl());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<MatchDto>>();
        result.Should().HaveCount(2);
        result![0].No.Should().Be(1);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoMatches()
    {
        AsUser();
        _factory.MatchRepository.GetByTournamentAsync(TournamentId).Returns(new List<Match>());

        var response = await _client.GetAsync(MatchesUrl());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<MatchDto>>();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.GetAsync(MatchesUrl());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Returns403_WhenMissingRole()
    {
        _factory.TestAuth.Roles = [];

        var response = await _client.GetAsync(MatchesUrl());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── GET /api/tournament/{id}/matches/{matchId} ───────────────────────────

    [Fact]
    public async Task GetById_ReturnsOk_WhenExists()
    {
        AsUser();
        _factory.MatchRepository.GetByIdAsync(1).Returns(MakeMatch(1));

        var response = await _client.GetAsync(MatchUrl(1));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MatchDto>();
        result!.Id.Should().Be(1);
        result.TournamentId.Should().Be(TournamentId);
        result.Round.Should().Be(1);
    }

    [Fact]
    public async Task GetById_Returns404_WhenMatchNotFound()
    {
        AsUser();
        _factory.MatchRepository.GetByIdAsync(999).Returns((Match?)null);

        var response = await _client.GetAsync(MatchUrl(999));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Returns404_WhenMatchBelongsToDifferentTournament()
    {
        AsUser();
        _factory.MatchRepository.GetByIdAsync(5).Returns(MakeMatch(5, tournamentId: 99));

        var response = await _client.GetAsync(MatchUrl(5, tournamentId: TournamentId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.GetAsync(MatchUrl(1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── POST /api/tournament/{id}/matches ────────────────────────────────────

    [Fact]
    public async Task Create_Returns201_WhenValidRequest()
    {
        AsAdmin();
        var dto     = new MatchDto(0, TournamentId, 1, 3, null, null, null, null, null, null, null, null);
        var created = MakeMatch(10);

        _ = _factory.MatchRepository.AddAsync(Arg.Do<Match>(m => m.Id = 10));
        _factory.MatchRepository.GetByIdAsync(10).Returns(created);

        var response = await _client.PostAsJsonAsync(MatchesUrl(), dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<MatchDto>();
        result!.Id.Should().Be(10);
        result.TournamentId.Should().Be(TournamentId);
    }

    [Fact]
    public async Task Create_Returns400_WhenIdNotZero()
    {
        AsAdmin();
        var dto = new MatchDto(5, TournamentId, 1, 1, null, null, null, null, null, null, null, null);

        var response = await _client.PostAsJsonAsync(MatchesUrl(), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns400_WhenTournamentIdMismatch()
    {
        AsAdmin();
        var dto = new MatchDto(0, 99, 1, 1, null, null, null, null, null, null, null, null);

        var response = await _client.PostAsJsonAsync(MatchesUrl(TournamentId), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns400_WhenRoundIsZero()
    {
        AsAdmin();
        var dto = new MatchDto(0, TournamentId, 0, 1, null, null, null, null, null, null, null, null);

        var response = await _client.PostAsJsonAsync(MatchesUrl(), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns400_WhenNoIsZero()
    {
        AsAdmin();
        var dto = new MatchDto(0, TournamentId, 1, 0, null, null, null, null, null, null, null, null);

        var response = await _client.PostAsJsonAsync(MatchesUrl(), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns403_WhenNotAdmin()
    {
        AsUser();
        var dto = new MatchDto(0, TournamentId, 1, 1, null, null, null, null, null, null, null, null);

        var response = await _client.PostAsJsonAsync(MatchesUrl(), dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();
        var dto = new MatchDto(0, TournamentId, 1, 1, null, null, null, null, null, null, null, null);

        var response = await _client.PostAsJsonAsync(MatchesUrl(), dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── PATCH /api/tournament/{id}/matches/{matchId} ─────────────────────────

    [Fact]
    public async Task Modify_Returns204_WhenValid()
    {
        AsAdmin();
        var entity = MakeMatch(1);
        _factory.MatchRepository.GetByIdAsync(1).Returns(entity);
        var dto = new MatchModifyDto(null, null, null, null, "WonB", "Great match");

        var response = await _client.PatchAsJsonAsync(MatchUrl(1), dto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        entity.Result.Should().Be(MatchResult.WonB);
        entity.Remark.Should().Be("Great match");
    }

    [Fact]
    public async Task Modify_Returns404_WhenMatchNotFound()
    {
        AsAdmin();
        _factory.MatchRepository.GetByIdAsync(999).Returns((Match?)null);
        var dto = new MatchModifyDto(null, null, null, null, "WonA", null);

        var response = await _client.PatchAsJsonAsync(MatchUrl(999), dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Modify_Returns404_WhenMatchBelongsToDifferentTournament()
    {
        AsAdmin();
        _factory.MatchRepository.GetByIdAsync(5).Returns(MakeMatch(5, tournamentId: 99));
        var dto = new MatchModifyDto(null, null, null, null, "WonA", null);

        var response = await _client.PatchAsJsonAsync(MatchUrl(5, TournamentId), dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Modify_Returns400_WhenRemarkTooLong()
    {
        AsAdmin();
        var dto = new MatchModifyDto(null, null, null, null, null, new string('x', 201));

        var response = await _client.PatchAsJsonAsync(MatchUrl(1), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Modify_Returns403_WhenNotAdmin()
    {
        AsUser();
        var dto = new MatchModifyDto(null, null, null, null, "WonA", null);

        var response = await _client.PatchAsJsonAsync(MatchUrl(1), dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Modify_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();
        var dto = new MatchModifyDto(null, null, null, null, "WonA", null);

        var response = await _client.PatchAsJsonAsync(MatchUrl(1), dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── PUT /api/tournament/{id}/matches/{matchId}/winner ────────────────────

    private static string WinnerUrl(int id, int tournamentId = TournamentId) =>
        $"/api/tournament/{tournamentId}/matches/{id}/winner";

    [Fact]
    public async Task SetWinner_Returns204_WhenWonA()
    {
        AsAdmin();
        var entity = MakeMatch(1);
        entity.TeamAId = 10;
        entity.TeamBId = 20;
        _factory.MatchRepository.GetByIdAsync(1).Returns(entity);

        var response = await _client.PutAsJsonAsync(WinnerUrl(1), new SetWinnerDto("WonA"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.MatchRepository.Received(1).SetWinnerAsync(1, MatchResult.WonA);
    }

    [Fact]
    public async Task SetWinner_Returns204_WhenWonB()
    {
        AsAdmin();
        var entity = MakeMatch(2);
        entity.TeamAId = 10;
        entity.TeamBId = 20;
        _factory.MatchRepository.GetByIdAsync(2).Returns(entity);

        var response = await _client.PutAsJsonAsync(WinnerUrl(2), new SetWinnerDto("WonB"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.MatchRepository.Received(1).SetWinnerAsync(2, MatchResult.WonB);
    }

    [Fact]
    public async Task SetWinner_Returns404_WhenMatchNotFound()
    {
        AsAdmin();
        _factory.MatchRepository.GetByIdAsync(999).Returns((Match?)null);

        var response = await _client.PutAsJsonAsync(WinnerUrl(999), new SetWinnerDto("WonA"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetWinner_Returns404_WhenMatchBelongsToDifferentTournament()
    {
        AsAdmin();
        _factory.MatchRepository.GetByIdAsync(5).Returns(MakeMatch(5, tournamentId: 99));

        var response = await _client.PutAsJsonAsync(WinnerUrl(5, TournamentId), new SetWinnerDto("WonA"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetWinner_Returns400_WhenInvalidWinner()
    {
        AsAdmin();
        _factory.MatchRepository.GetByIdAsync(1).Returns(MakeMatch(1));

        var response = await _client.PutAsJsonAsync(WinnerUrl(1), new SetWinnerDto("Draw"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetWinner_Returns400_WhenMatchAlreadyFinished()
    {
        AsAdmin();
        _factory.MatchRepository.GetByIdAsync(1).Returns(MakeMatch(1));
        _factory.MatchRepository
            .SetWinnerAsync(1, MatchResult.WonA)
            .Returns(Task.FromException(new InvalidOperationException("The match already ended! 1")));

        var response = await _client.PutAsJsonAsync(WinnerUrl(1), new SetWinnerDto("WonA"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetWinner_Returns403_WhenNotAdmin()
    {
        AsUser();

        var response = await _client.PutAsJsonAsync(WinnerUrl(1), new SetWinnerDto("WonA"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetWinner_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.PutAsJsonAsync(WinnerUrl(1), new SetWinnerDto("WonA"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── No DELETE or PUT allowed ─────────────────────────────────────────────

    [Fact]
    public async Task Delete_Returns405_MethodNotAllowed()
    {
        AsAdmin();

        var response = await _client.DeleteAsync(MatchUrl(1));

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Put_Returns405_MethodNotAllowed()
    {
        AsAdmin();
        var dto = new MatchDto(1, TournamentId, 1, 1, null, null, null, null, null, null, null, null);

        var response = await _client.PutAsJsonAsync(MatchUrl(1), dto);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}

using System.Net;
using System.Net.Http.Json;

using Core.Entities;

using FluentAssertions;

using NSubstitute;

using WebAPI;
using WebAPI.Endpoints;

namespace WebAPI.Tests.Endpoints;

public class TeamEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    private const int TournamentId = 1;

    public TeamEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Reset();
        _client = factory.CreateClient();
    }

    private void AsAdmin()           => _factory.TestAuth.Roles = [Settings.KeycloakAdminRoleName];
    private void AsUser()            => _factory.TestAuth.Roles = [Settings.KeycloakUserRoleName];
    private void AsUnauthenticated() => _factory.TestAuth.IsAuthenticated = false;

    private static string TeamsUrl(int tournamentId = TournamentId) =>
        $"/api/tournament/{tournamentId}/teams";

    private static string TeamUrl(int id, int tournamentId = TournamentId) =>
        $"/api/tournament/{tournamentId}/teams/{id}";

    private static Team MakeTeam(int id, int tournamentId = TournamentId) => new()
    {
        Id               = id,
        TournamentId     = tournamentId,
        Player1          = $"Team {id}",
        RegistrationDate = new DateTime(2025, 1, 10)
    };

    // ─── GET /api/tournament/{id}/teams ──────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList_WhenAuthenticated()
    {
        AsUser();
        var teams = new List<Team> { MakeTeam(1), MakeTeam(2) };
        _factory.TeamRepository.GetByTournamentAsync(TournamentId).Returns(teams);

        var response = await _client.GetAsync(TeamsUrl());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        result.Should().HaveCount(2);
        result![0].Name.Should().Be("Team 1");
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoTeams()
    {
        AsUser();
        _factory.TeamRepository.GetByTournamentAsync(TournamentId).Returns(new List<Team>());

        var response = await _client.GetAsync(TeamsUrl());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.GetAsync(TeamsUrl());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Returns403_WhenMissingRole()
    {
        _factory.TestAuth.Roles = [];

        var response = await _client.GetAsync(TeamsUrl());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── GET /api/tournament/{id}/teams/{teamId} ──────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOk_WhenExists()
    {
        AsUser();
        _factory.TeamRepository.GetByIdAsync(1).Returns(MakeTeam(1));

        var response = await _client.GetAsync(TeamUrl(1));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TeamDto>();
        result!.Id.Should().Be(1);
        result.TournamentId.Should().Be(TournamentId);
        result.Name.Should().Be("Team 1");
    }

    [Fact]
    public async Task GetById_Returns404_WhenTeamNotFound()
    {
        AsUser();
        _factory.TeamRepository.GetByIdAsync(999).Returns((Team?)null);

        var response = await _client.GetAsync(TeamUrl(999));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Returns404_WhenTeamBelongsToDifferentTournament()
    {
        AsUser();
        // Team 5 belongs to tournament 99, not tournament 1
        _factory.TeamRepository.GetByIdAsync(5).Returns(MakeTeam(5, tournamentId: 99));

        var response = await _client.GetAsync(TeamUrl(5, tournamentId: TournamentId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.GetAsync(TeamUrl(1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── POST /api/tournament/{id}/teams ──────────────────────────────────────

    [Fact]
    public async Task Create_Returns201_WhenValidRequest()
    {
        AsAdmin();
        var dto     = new TeamDto(0, TournamentId, "New Team", null, "", 3, null, DateTime.Now, null);
        var created = MakeTeam(10);

        _ = _factory.TeamRepository.AddAsync(Arg.Do<Team>(t => t.Id = 10));
        _factory.TeamRepository.GetByIdAsync(10).Returns(created);

        var response = await _client.PostAsJsonAsync(TeamsUrl(), dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TeamDto>();
        result!.Name.Should().Be("Team 10");
    }

    [Fact]
    public async Task Create_Returns400_WhenIdNotZero()
    {
        AsAdmin();
        var dto = new TeamDto(5, TournamentId, "New Team", null, "", null, null, DateTime.Now, null);

        var response = await _client.PostAsJsonAsync(TeamsUrl(), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns400_WhenTournamentIdMismatch()
    {
        AsAdmin();
        var dto = new TeamDto(0, 99, "New Team", null, "", null, null, DateTime.Now, null);

        var response = await _client.PostAsJsonAsync(TeamsUrl(TournamentId), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns400_WhenPlayer1Empty()
    {
        AsAdmin();
        var dto = new TeamDto(0, TournamentId, "", null, "", null, null, DateTime.Now, null);

        var response = await _client.PostAsJsonAsync(TeamsUrl(), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns403_WhenNotAdmin()
    {
        AsUser();
        var dto = new TeamDto(0, TournamentId, "New Team", null, "", null, null, DateTime.Now, null);

        var response = await _client.PostAsJsonAsync(TeamsUrl(), dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();
        var dto = new TeamDto(0, TournamentId, "New Team", null, "", null, null, DateTime.Now, null);

        var response = await _client.PostAsJsonAsync(TeamsUrl(), dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── PUT /api/tournament/{id}/teams/{teamId} ──────────────────────────────

    [Fact]
    public async Task Update_Returns204_WhenValid()
    {
        AsAdmin();
        var entity = MakeTeam(1);
        _factory.TeamRepository.GetByIdAsync(1).Returns(entity);
        var dto = new TeamDto(1, TournamentId, "Updated Name", null, "", 5, null, entity.RegistrationDate, null);

        var response = await _client.PutAsJsonAsync(TeamUrl(1), dto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        entity.Name.Should().Be("Updated Name");
        entity.Seed.Should().Be(5);
    }

    [Fact]
    public async Task Update_Returns400_WhenIdMismatch()
    {
        AsAdmin();
        var dto = new TeamDto(2, TournamentId, "Updated Name", null, "", null, null, DateTime.Now, null);

        var response = await _client.PutAsJsonAsync(TeamUrl(1), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Returns400_WhenTournamentIdMismatch()
    {
        AsAdmin();
        var dto = new TeamDto(1, 99, "Updated Name", null, "", null, null, DateTime.Now, null);

        var response = await _client.PutAsJsonAsync(TeamUrl(1, TournamentId), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Returns400_WhenTeamNotFound()
    {
        AsAdmin();
        _factory.TeamRepository.GetByIdAsync(999).Returns((Team?)null);
        var dto = new TeamDto(999, TournamentId, "Updated Name", null, "", null, null, DateTime.Now, null);

        var response = await _client.PutAsJsonAsync(TeamUrl(999), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Returns400_WhenTeamBelongsToDifferentTournament()
    {
        AsAdmin();
        _factory.TeamRepository.GetByIdAsync(5).Returns(MakeTeam(5, tournamentId: 99));
        var dto = new TeamDto(5, TournamentId, "Updated Name", null, "", null, null, DateTime.Now, null);

        var response = await _client.PutAsJsonAsync(TeamUrl(5, TournamentId), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Returns400_WhenPlayer1Empty()
    {
        AsAdmin();
        var dto = new TeamDto(1, TournamentId, "", null, "", null, null, DateTime.Now, null);

        var response = await _client.PutAsJsonAsync(TeamUrl(1), dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Returns403_WhenNotAdmin()
    {
        AsUser();
        var dto = new TeamDto(1, TournamentId, "Updated Name", null, "", null, null, DateTime.Now, null);

        var response = await _client.PutAsJsonAsync(TeamUrl(1), dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── DELETE /api/tournament/{id}/teams/{teamId} ───────────────────────────

    [Fact]
    public async Task Delete_Returns204_WhenExists()
    {
        AsAdmin();
        _factory.TeamRepository.GetByIdAsync(1).Returns(MakeTeam(1));

        var response = await _client.DeleteAsync(TeamUrl(1));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Returns400_WhenTeamNotFound()
    {
        AsAdmin();
        _factory.TeamRepository.GetByIdAsync(999).Returns((Team?)null);

        var response = await _client.DeleteAsync(TeamUrl(999));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_Returns400_WhenTeamBelongsToDifferentTournament()
    {
        AsAdmin();
        _factory.TeamRepository.GetByIdAsync(5).Returns(MakeTeam(5, tournamentId: 99));

        var response = await _client.DeleteAsync(TeamUrl(5, TournamentId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_Returns403_WhenNotAdmin()
    {
        AsUser();

        var response = await _client.DeleteAsync(TeamUrl(1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.DeleteAsync(TeamUrl(1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

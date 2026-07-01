using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using NSubstitute;

using WebAPI;
using WebAPI.Endpoints;

namespace WebAPI.Tests.Endpoints;

using Persistence.Model;

using Shared.Exceptions;

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
    private void AsMissingRole()     => _factory.TestAuth.Roles = [];

    private static string TeamsUrl(int tournamentId = TournamentId) =>
        $"/api/tournament/{tournamentId}/teams";

    private static string TeamUrl(int id, int tournamentId = TournamentId) =>
        $"/api/tournament/{tournamentId}/teams/{id}";

    private static Team MakeTeam(int id, int tournamentId = TournamentId) => new()
    {
        Id               = id,
        TournamentId     = tournamentId,
        Player1          = $"Team {id}",
        RegistrationDate = new DateTime(2025, 1, 10),
        RegistrationCode = "00000"
    };

    private static Tournament MakeTournament(IList<Team> teams) => new()
    {
        Id              = TournamentId,
        Description     = "Spring Cup",
        From            = new DateOnly(2025, 1, 1),
        RegistrationPin = "12345",
        Teams           = teams
    };

    // ─── GET /api/tournament/{id}/teams ──────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList_WhenAuthenticated()
    {
        AsUser();
        var teams = new List<Team> { MakeTeam(1), MakeTeam(2) };
        _factory.TournamentService.SingleTournamentAsync(TournamentId, nameof(Tournament.Teams)).Returns(MakeTournament(teams));

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
        _factory.TournamentService.SingleTournamentAsync(TournamentId, nameof(Tournament.Teams)).Returns(MakeTournament([]));

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
        AsMissingRole();

        var response = await _client.GetAsync(TeamsUrl());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── GET /api/tournament/{id}/teams/{teamId} ──────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOk_WhenExists()
    {
        AsUser();
        _factory.TeamService.SingleTeamAsync(1).Returns(MakeTeam(1));

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
        _factory.TeamService.SingleTeamAsync(999)
            .Returns(Task.FromException<Team>(new NotFoundException("Team 999 not found")));

        var response = await _client.GetAsync(TeamUrl(999));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Returns400_WhenTeamBelongsToDifferentTournament()
    {
        AsUser();
        // Team 5 belongs to tournament 99, not tournament 1
        _factory.TeamService.SingleTeamAsync(5).Returns(MakeTeam(5, tournamentId: 99));

        var response = await _client.GetAsync(TeamUrl(5, tournamentId: TournamentId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

        _factory.TournamentService.RegisterTeamAsync(TournamentId, "New Team", 3, null)
            .Returns(new Team { Id = 10, TournamentId = TournamentId, Player1 = "New Team", RegistrationCode = "00000" });
        _factory.TeamService.GetTeamByIdAsync(10).Returns(created);

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
    public async Task Create_Returns403_WhenMissingRole()
    {
        AsMissingRole();
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
        var dto = new TeamDto(1, TournamentId, "Updated Name", null, "", 5, null, DateTime.Now, null);

        var response = await _client.PutAsJsonAsync(TeamUrl(1), dto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.TeamService.Received(1).UpdateTeamAsync(1, TournamentId, "Updated Name", null, 5, null);
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
    public async Task Update_Returns404_WhenTeamNotFound()
    {
        AsAdmin();
        _factory.TeamService.UpdateTeamAsync(999, TournamentId, "Updated Name", null, null, null)
            .Returns(Task.FromException(new NotFoundException("Team 999 not found")));
        var dto = new TeamDto(999, TournamentId, "Updated Name", null, "", null, null, DateTime.Now, null);

        var response = await _client.PutAsJsonAsync(TeamUrl(999), dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns404_WhenTeamBelongsToDifferentTournament()
    {
        AsAdmin();
        _factory.TeamService.UpdateTeamAsync(5, TournamentId, "Updated Name", null, null, null)
            .Returns(Task.FromException(new NotFoundException("Team 5 not found in tournament 1")));
        var dto = new TeamDto(5, TournamentId, "Updated Name", null, "", null, null, DateTime.Now, null);

        var response = await _client.PutAsJsonAsync(TeamUrl(5, TournamentId), dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
    public async Task Update_Returns403_WhenMissingRole()
    {
        AsMissingRole();
        var dto = new TeamDto(1, TournamentId, "Updated Name", null, "", null, null, DateTime.Now, null);

        var response = await _client.PutAsJsonAsync(TeamUrl(1), dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── DELETE /api/tournament/{id}/teams/{teamId} ───────────────────────────

    [Fact]
    public async Task Delete_Returns204_WhenExists()
    {
        AsAdmin();

        var response = await _client.DeleteAsync(TeamUrl(1));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.TeamService.Received(1).DeleteTeamAsync(1, TournamentId);
    }

    [Fact]
    public async Task Delete_Returns404_WhenTeamNotFound()
    {
        AsAdmin();
        _factory.TeamService.DeleteTeamAsync(999, TournamentId)
            .Returns(Task.FromException(new NotFoundException("Team 999 not found")));

        var response = await _client.DeleteAsync(TeamUrl(999));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Returns404_WhenTeamBelongsToDifferentTournament()
    {
        AsAdmin();
        _factory.TeamService.DeleteTeamAsync(5, TournamentId)
            .Returns(Task.FromException(new NotFoundException("Team 5 not found in tournament 1")));

        var response = await _client.DeleteAsync(TeamUrl(5, TournamentId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Returns403_WhenMissingRole()
    {
        AsMissingRole();

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

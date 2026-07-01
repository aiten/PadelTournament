using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using NSubstitute;

using WebAPI;
using WebAPI.Endpoints;

namespace WebAPI.Tests.Endpoints;

using Persistence.Model;
using Persistence.QueryResult;

using Shared.Exceptions;

public class TournamentEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    public TournamentEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Reset();
        _client = factory.CreateClient();
    }

    private void AsAdmin()            => _factory.TestAuth.Roles = [Settings.KeycloakAdminRoleName];
    private void AsUser()             => _factory.TestAuth.Roles = [Settings.KeycloakUserRoleName];
    private void AsUnauthenticated()  => _factory.TestAuth.IsAuthenticated = false;
    private void AsMissingRole()      => _factory.TestAuth.Roles = [];

    // ─── GET /api/tournament ────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList_WhenAuthenticated()
    {
        AsUser();
        var overviews = new List<TournamentOverview>
        {
            new(1, "Spring Cup",  "12345", new DateOnly(2024, 3,  1), null,                   4, 2, 1),
            new(2, "Summer Open", "23456", new DateOnly(2024, 7, 1), new DateOnly(2024, 7, 7), 8, 0, 0),
        };
        _factory.TournamentService.GetTournamentOverviewsAsync().Returns(overviews);

        var response = await _client.GetAsync("/api/tournament");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TournamentOverview>>();
        result.Should().HaveCount(2);
        result![0].Description.Should().Be("Spring Cup");
    }

    [Fact]
    public async Task GetAll_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.GetAsync("/api/tournament");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Returns403_WhenMissingRole()
    {
        AsMissingRole(); // authenticated but no role

        var response = await _client.GetAsync("/api/tournament");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── GET /api/tournament/{id} ────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOk_WhenExists()
    {
        AsUser();
        var tournament = new Tournament { Id = 1, Description = "Spring Cup", From = new DateOnly(2024, 3, 1), RegistrationPin = "12345" };
        _factory.TournamentService.SingleTournamentAsync(1).Returns(tournament);

        var response = await _client.GetAsync("/api/tournament/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.Id.Should().Be(1);
        result.Description.Should().Be("Spring Cup");
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        AsUser();
        _factory.TournamentService.SingleTournamentAsync(999)
            .Returns(Task.FromException<Tournament>(new NotFoundException("Tournament 999 not found")));

        var response = await _client.GetAsync("/api/tournament/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.GetAsync("/api/tournament/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── POST /api/tournament ────────────────────────────────────────────────

    [Fact]
    public async Task Create_Returns201_WhenValidRequest()
    {
        AsAdmin();
        var dto     = new TournamentDto(0, "New Cup", new DateOnly(2025, 1, 1), null, "54321");
        var created = new Tournament { Id = 1, Description = "New Cup", From = new DateOnly(2025, 1, 1), RegistrationPin = "54321" };

        _factory.TournamentService.AddTournamentAsync(Arg.Any<Tournament>()).Returns(created);

        var response = await _client.PostAsJsonAsync("/api/tournament", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TournamentDto>();
        result!.Description.Should().Be("New Cup");
    }

    [Fact]
    public async Task Create_Returns400_WhenIdNotZero()
    {
        AsAdmin();
        var dto = new TournamentDto(5, "New Cup", new DateOnly(2025, 1, 1), null, "54321");

        var response = await _client.PostAsJsonAsync("/api/tournament", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns400_WhenDescriptionEmpty()
    {
        AsAdmin();
        var dto = new TournamentDto(0, "", new DateOnly(2025, 1, 1), null, "54321");

        var response = await _client.PostAsJsonAsync("/api/tournament", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns403_WhenMissingRole()
    {
        AsMissingRole();
        var dto = new TournamentDto(0, "New Cup", new DateOnly(2025, 1, 1), null, "54321");

        var response = await _client.PostAsJsonAsync("/api/tournament", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();
        var dto = new TournamentDto(0, "New Cup", new DateOnly(2025, 1, 1), null, "54321");

        var response = await _client.PostAsJsonAsync("/api/tournament", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── PUT /api/tournament/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task Update_Returns204_WhenValid()
    {
        AsAdmin();
        var dto = new TournamentDto(1, "Updated Name", new DateOnly(2024, 1, 1), null, "54321");

        var response = await _client.PutAsJsonAsync("/api/tournament/1", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.TournamentService.Received(1).UpdateTournamentAsync(1, Arg.Any<Tournament>());
    }

    [Fact]
    public async Task Update_Returns400_WhenIdMismatch()
    {
        AsAdmin();
        var dto = new TournamentDto(2, "Updated", new DateOnly(2024, 1, 1), null, "54321");

        var response = await _client.PutAsJsonAsync("/api/tournament/1", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Returns404_WhenTournamentNotFound()
    {
        AsAdmin();
        _factory.TournamentService.UpdateTournamentAsync(999, Arg.Any<Tournament>())
            .Returns(Task.FromException(new NotFoundException("Tournament 999 not found")));
        var dto = new TournamentDto(999, "Updated", new DateOnly(2024, 1, 1), null, "54321");

        var response = await _client.PutAsJsonAsync("/api/tournament/999", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns400_WhenDescriptionEmpty()
    {
        AsAdmin();
        var dto = new TournamentDto(1, "", new DateOnly(2024, 1, 1), null, "54321");

        var response = await _client.PutAsJsonAsync("/api/tournament/1", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Returns403_WhenMissingRole()
    {
        AsMissingRole();
        var dto = new TournamentDto(1, "Updated", new DateOnly(2024, 1, 1), null, "54321");

        var response = await _client.PutAsJsonAsync("/api/tournament/1", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── DELETE /api/tournament/{id} ─────────────────────────────────────────

    [Fact]
    public async Task Delete_Returns204_WhenExists()
    {
        AsAdmin();

        var response = await _client.DeleteAsync("/api/tournament/1");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.TournamentService.Received(1).DeleteTournamentAsync(1);
    }

    [Fact]
    public async Task Delete_Returns404_WhenNotFound()
    {
        AsAdmin();
        _factory.TournamentService.DeleteTournamentAsync(999)
            .Returns(Task.FromException(new NotFoundException("Tournament 999 not found")));

        var response = await _client.DeleteAsync("/api/tournament/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Returns403_WhenMissingRole()
    {
        AsMissingRole();

        var response = await _client.DeleteAsync("/api/tournament/1");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.DeleteAsync("/api/tournament/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── POST /api/tournament/{id}/generate-schedule ─────────────────────────

    private static string GenerateBracketUrl(int id = 1) => $"/api/tournament/{id}/generate-schedule";

    [Fact]
    public async Task GenerateBracket_Returns200_WithMatchesFromService()
    {
        AsAdmin();
        var tournament = new Tournament
        {
            Id              = 1,
            Description     = "Spring Cup",
            From            = new DateOnly(2025, 1, 1),
            RegistrationPin = "12345",
            Matches = new List<Match>
            {
                new() { Id = 1, TournamentId = 1, Round = 1, No = 1, TeamAId = 1, TeamBId = 2, NextMatchId = 3 },
                new() { Id = 2, TournamentId = 1, Round = 1, No = 2, TeamAId = 3, TeamBId = 4, NextMatchId = 3 },
                new() { Id = 3, TournamentId = 1, Round = 2, No = 1 },
            }
        };
        _factory.TournamentService.GenerateMatchScheduleAsync(1).Returns(tournament);

        var response = await _client.PostAsync(GenerateBracketUrl(), null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<MatchDto>>();
        result.Should().HaveCount(3);

        var round1 = result!.Where(m => m.Round == 1).OrderBy(m => m.No).ToList();
        var round2 = result!.Where(m => m.Round == 2).ToList();

        round1[0].TeamAId.Should().Be(1);
        round1[0].TeamBId.Should().Be(2);
        round1[0].NextMatchId.Should().Be(3);
        round1[1].TeamAId.Should().Be(3);
        round1[1].TeamBId.Should().Be(4);
        round1[1].NextMatchId.Should().Be(3);

        round2[0].TeamAId.Should().BeNull();
        round2[0].TeamBId.Should().BeNull();
        round2[0].NextMatchId.Should().BeNull();
    }

    [Fact]
    public async Task GenerateBracket_Returns404_WhenTournamentNotFound()
    {
        AsAdmin();
        _factory.TournamentService.GenerateMatchScheduleAsync(999)
            .Returns(Task.FromException<Tournament>(new NotFoundException("Tournament 999 not found")));

        var response = await _client.PostAsync(GenerateBracketUrl(999), null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateBracket_Returns422_WhenBracketAlreadyExists()
    {
        AsAdmin();
        _factory.TournamentService.GenerateMatchScheduleAsync(1)
            .Returns(Task.FromException<Tournament>(
                new InvalidTournamentDataException("Matches already exist for this tournament")));

        var response = await _client.PostAsync(GenerateBracketUrl(), null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GenerateBracket_Returns422_WhenFewerThan2Teams()
    {
        AsAdmin();
        _factory.TournamentService.GenerateMatchScheduleAsync(1)
            .Returns(Task.FromException<Tournament>(
                new InvalidTournamentDataException("At least 2 teams must be registered before generating a bracket")));

        var response = await _client.PostAsync(GenerateBracketUrl(), null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GenerateBracket_Returns403_WhenMissingRole()
    {
        AsMissingRole();

        var response = await _client.PostAsync(GenerateBracketUrl(), null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateBracket_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.PostAsync(GenerateBracketUrl(), null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

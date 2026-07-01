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

public class MatchResultEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    private const int TournamentId = 1;
    private const int MatchId      = 10;

    public MatchResultEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Reset();
        _client = factory.CreateClient();
    }

    private void AsAdmin()           => _factory.TestAuth.Roles = [Settings.KeycloakAdminRoleName];
    private void AsUser()            => _factory.TestAuth.Roles = [Settings.KeycloakUserRoleName];
    private void AsUnauthenticated() => _factory.TestAuth.IsAuthenticated = false;
    private void AsMissingRole()     => _factory.TestAuth.Roles = [];

    private static string ResultUrl(int matchId = MatchId, int tournamentId = TournamentId) =>
        $"/api/tournament/{tournamentId}/matches/{matchId}/result";

    private static Match MakeMatch(int id, int tournamentId = TournamentId) => new()
    {
        Id           = id,
        TournamentId = tournamentId,
        Round        = 1,
        No           = id,
    };

    private static MatchResultOverview MakeMatchResult(int matchId = MatchId, MatchResult? result = MatchResult.WonA) =>
        new(
            matchId,
            "Team A",
            "Team B",
            result,
            new List<SetResultOverview>
            {
                new(1, 6, 3, null,
                    new List<GameResultOverview>
                    {
                        new(1, null,"15-0"),
                        new(2, null,"15-15"),
                    }),
                new(2, 6, 4, null,
                    new List<GameResultOverview>
                    {
                        new(1, null,"15-0"),
                    }),
            }
        );

    // ─── GET /api/tournament/{id}/matches/{matchId}/result ────────────────────

    [Fact]
    public async Task Get_ReturnsOk_WhenMatchExistsWithResult()
    {
        AsUser();
        _factory.MatchService.SingleMatchAsync(MatchId).Returns(MakeMatch(MatchId));
        _factory.MatchService.GetMatchResultAsync(MatchId).Returns(MakeMatchResult());

        var response = await _client.GetAsync(ResultUrl());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MatchResultDto>();
        result!.Id.Should().Be(MatchId);
        result.Result.Should().Be(MatchResult.WonA.ToString());
        result.Sets.Should().HaveCount(2);
        result.Sets[0].No.Should().Be(1);
        result.Sets[0].Result.Should().Be("6-3");
        result.Sets[1].Result.Should().Be("6-4");
    }

    [Fact]
    public async Task Get_ReturnsOkWithTieBreak_WhenSetHasTieBreak()
    {
        AsUser();
        _factory.MatchService.SingleMatchAsync(MatchId).Returns(MakeMatch(MatchId));
        var overview = new MatchResultOverview(
            MatchId, "Team A", "Team B", MatchResult.WonA,
            new List<SetResultOverview>
            {
                new(1, 7, 6, 5, new List<GameResultOverview>()),
            });
        _factory.MatchService.GetMatchResultAsync(MatchId).Returns(overview);

        var response = await _client.GetAsync(ResultUrl());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MatchResultDto>();
        result!.Sets[0].Result.Should().Be("7-6(5)");
    }

    [Fact]
    public async Task Get_ReturnsOkWithNullResult_WhenMatchHasNoResult()
    {
        AsUser();
        _factory.MatchService.SingleMatchAsync(MatchId).Returns(MakeMatch(MatchId));
        _factory.MatchService.GetMatchResultAsync(MatchId).Returns(MakeMatchResult(result: null));

        var response = await _client.GetAsync(ResultUrl());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MatchResultDto>();
        result!.Result.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Get_Returns404_WhenMatchNotFound()
    {
        AsUser();
        _factory.MatchService.SingleMatchAsync(999)
            .Returns(Task.FromException<Match>(new NotFoundException("Match 999 not found")));

        var response = await _client.GetAsync(ResultUrl(matchId: 999));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Returns400_WhenMatchBelongsToDifferentTournament()
    {
        AsUser();
        _factory.MatchService.SingleMatchAsync(MatchId).Returns(MakeMatch(MatchId, tournamentId: 99));

        var response = await _client.GetAsync(ResultUrl(matchId: MatchId, tournamentId: TournamentId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.GetAsync(ResultUrl());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Returns403_WhenMissingRole()
    {
        AsMissingRole();

        var response = await _client.GetAsync(ResultUrl());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── PUT /api/tournament/{id}/matches/{matchId}/result ────────────────────

    private static MatchResultDto MakeResultDto() =>
        new(
            MatchId,
            "WonA",
            new List<SetResultDto>
            {
                new(1, "6-3", ["15-0", "15-15"]),
                new(2, "6-4", ["15-0"]),
            }
        );

    [Fact]
    public async Task Put_Returns204_WhenAdminUpdatesResult()
    {
        AsAdmin();
        _factory.MatchService.SingleMatchAsync(MatchId).Returns(MakeMatch(MatchId));

        var response = await _client.PutAsJsonAsync(ResultUrl(), MakeResultDto());

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.MatchService.Received(1).UpdateMatchResultAsync(Arg.Is(MatchId), Arg.Any<MatchResultOverview>());
    }

    [Fact]
    public async Task Put_UpdatesCorrectSets_WhenResultHasTieBreak()
    {
        AsAdmin();
        _factory.MatchService.SingleMatchAsync(MatchId).Returns(MakeMatch(MatchId));
        var dto = new MatchResultDto(
            MatchId, "WonA",
            new List<SetResultDto>
            {
                new(1, "7-6(5)", []),
            }
        );

        MatchResultOverview? captured = null;
        await _factory.MatchService.UpdateMatchResultAsync(
            Arg.Any<int>(),
            Arg.Do<MatchResultOverview>(r => captured = r));

        var response = await _client.PutAsJsonAsync(ResultUrl(), dto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        captured!.SetResults.First().ScoreA.Should().Be(7);
        captured.SetResults.First().ScoreB.Should().Be(6);
        captured.SetResults.First().TieBreakPoints.Should().Be(5);
    }

    [Fact]
    public async Task Put_Returns404_WhenMatchNotFound()
    {
        AsAdmin();
        _factory.MatchService.SingleMatchAsync(999)
            .Returns(Task.FromException<Match>(new NotFoundException("Match 999 not found")));

        var response = await _client.PutAsJsonAsync(ResultUrl(matchId: 999), MakeResultDto());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_Returns400_WhenMatchBelongsToDifferentTournament()
    {
        AsAdmin();
        _factory.MatchService.SingleMatchAsync(MatchId).Returns(MakeMatch(MatchId, tournamentId: 99));

        var response = await _client.PutAsJsonAsync(ResultUrl(matchId: MatchId, tournamentId: TournamentId), MakeResultDto());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_Returns403_WhenMissingRole()
    {
        AsMissingRole();

        var response = await _client.PutAsJsonAsync(ResultUrl(), MakeResultDto());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.PutAsJsonAsync(ResultUrl(), MakeResultDto());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── DELETE /api/tournament/{id}/matches/{matchId}/result ─────────────────

    [Fact]
    public async Task Delete_Returns204_WhenAdminDeletesResult()
    {
        AsAdmin();
        _factory.MatchService.SingleMatchAsync(MatchId).Returns(MakeMatch(MatchId));

        var response = await _client.DeleteAsync(ResultUrl());

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.MatchService.Received(1).DeleteMatchResultAsync(MatchId);
    }

    [Fact]
    public async Task Delete_Returns404_WhenMatchNotFound()
    {
        AsAdmin();
        _factory.MatchService.SingleMatchAsync(999)
            .Returns(Task.FromException<Match>(new NotFoundException("Match 999 not found")));

        var response = await _client.DeleteAsync(ResultUrl(matchId: 999));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Returns400_WhenMatchBelongsToDifferentTournament()
    {
        AsAdmin();
        _factory.MatchService.SingleMatchAsync(MatchId).Returns(MakeMatch(MatchId, tournamentId: 99));

        var response = await _client.DeleteAsync(ResultUrl(matchId: MatchId, tournamentId: TournamentId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_Returns403_WhenMissingRole()
    {
        AsMissingRole();

        var response = await _client.DeleteAsync(ResultUrl());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Returns401_WhenNotAuthenticated()
    {
        AsUnauthenticated();

        var response = await _client.DeleteAsync(ResultUrl());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

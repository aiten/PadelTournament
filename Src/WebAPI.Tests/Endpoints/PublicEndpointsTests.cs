using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using NSubstitute;

using WebAPI;
using WebAPI.Endpoints;

namespace WebAPI.Tests.Endpoints;

using Persistence.Model;
using Persistence.QueryResult;

public class PublicEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient                  _client;

    private const string Pin              = "1234";
    private const string RegistrationCode = "abc123";
    private const int    TeamId           = 5;
    private const int    OpponentTeamId   = 6;
    private const int    MatchId          = 10;

    public PublicEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Reset();
        _client = factory.CreateClient();
    }

    private static string ResultUrl(int matchId = MatchId) =>
        $"/api/public/{Pin}/{RegistrationCode}/matches/{matchId}/result";

    private static Team MakeTeam(int id = TeamId) => new()
    {
        Id               = id,
        Player1          = "Player A",
        RegistrationCode = RegistrationCode,
    };

    private static Match MakeMatch(int id = MatchId, int teamAId = TeamId, int teamBId = OpponentTeamId) => new()
    {
        Id      = id,
        TeamAId = teamAId,
        TeamBId = teamBId,
        Round   = 1,
        No      = 1,
    };

    private void SetUpTeamAndMatch()
    {
        _factory.TeamService.SingleByRegistrationAsync(Pin, RegistrationCode).Returns(MakeTeam());
        _factory.MatchService.SingleMatchForTeamAsync(MatchId, TeamId).Returns(MakeMatch());
    }

    [Fact]
    public async Task Put_Returns204_WhenOnlyWonIsProvided()
    {
        SetUpTeamAndMatch();

        var response = await _client.PutAsJsonAsync(ResultUrl(), new PublicMatchResultDto(true));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.MatchService.Received(1).AcceptResultAsync(MatchId, true, MatchResult.WonA, null);
        await _factory.MatchService.DidNotReceive().UpdateMatchResultAsync(Arg.Any<int>(), Arg.Any<MatchResultOverview>());
    }

    [Fact]
    public async Task Put_UpdatesSets_WhenResultScoreIsProvided()
    {
        SetUpTeamAndMatch();

        MatchResultOverview? captured = null;
        await _factory.MatchService.UpdateMatchResultAsync(
            Arg.Any<int>(),
            Arg.Do<MatchResultOverview>(r => captured = r));

        var response = await _client.PutAsJsonAsync(ResultUrl(), new PublicMatchResultDto(true, "6:4, 6:3, 6:5(2)"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _factory.MatchService.Received(1).UpdateMatchResultAsync(MatchId, Arg.Any<MatchResultOverview>());

        captured.Should().NotBeNull();
        captured!.SetResults.Should().HaveCount(3);
        captured.SetResults.ElementAt(0).ScoreA.Should().Be(6);
        captured.SetResults.ElementAt(0).ScoreB.Should().Be(4);
        captured.SetResults.ElementAt(0).TieBreakPoints.Should().BeNull();
        captured.SetResults.ElementAt(2).ScoreA.Should().Be(6);
        captured.SetResults.ElementAt(2).ScoreB.Should().Be(5);
        captured.SetResults.ElementAt(2).TieBreakPoints.Should().Be(2);
    }

    [Fact]
    public async Task Put_Returns400_WhenResultScoreIsMalformed()
    {
        SetUpTeamAndMatch();

        var response = await _client.PutAsJsonAsync(ResultUrl(), new PublicMatchResultDto(true, "not-a-score"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await _factory.MatchService.DidNotReceive().AcceptResultAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<MatchResult>(), Arg.Any<IList<SetResultOverview>?>());
    }
}

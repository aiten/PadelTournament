
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using WebAPI;

namespace WebAPI.Tests;

using Base.Persistence.Contracts;

using Persistence;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public IUnitOfWork           UnitOfWork           { get; } = Substitute.For<IUnitOfWork>();
    public ITournamentRepository TournamentRepository { get; } = Substitute.For<ITournamentRepository>();
    public ITeamRepository       TeamRepository       { get; } = Substitute.For<ITeamRepository>();
    public IMatchRepository      MatchRepository      { get; } = Substitute.For<IMatchRepository>();
    public TestAuthContext        TestAuth             { get; } = new();

    public CustomWebApplicationFactory()
    {
        UnitOfWork.Tournaments.Returns(TournamentRepository);
        UnitOfWork.Teams.Returns(TeamRepository);
        UnitOfWork.Matches.Returns(MatchRepository);
        UnitOfWork.BeginTransactionAsync().Returns(Substitute.For<ITransaction>());
    }

    /// <summary>
    /// Call at the start of each test to reset shared state (auth and received-call history).
    /// Return-value configurations carry over intentionally — each test re-configures what it needs.
    /// </summary>
    public void Reset()
    {
        TournamentRepository.ClearReceivedCalls();
        TeamRepository.ClearReceivedCalls();
        MatchRepository.ClearReceivedCalls();
        TestAuth.IsAuthenticated = true;
        TestAuth.Roles           = [];
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext, UnitOfWork, and all repository registrations
            var persistenceAssembly = typeof(Persistence.ApplicationDbContext).Assembly;
            var descriptors = services
                .Where(d => d.ServiceType == typeof(IUnitOfWork) ||
                            (d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("DbContext")) ||
                            (d.ImplementationType != null && d.ImplementationType.Assembly == persistenceAssembly))
                .ToList();

            foreach (var descriptor in descriptors)
                services.Remove(descriptor);

            // Register test auth context and replace Keycloak JWT with a simple test scheme
            services.AddSingleton(TestAuth);
            services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            // Override Keycloak resource-role policies with plain RequireRole checks
            services.AddAuthorizationBuilder()
                    .AddPolicy(Settings.AdminPolicyName,
                        p => p.RequireAuthenticatedUser()
                               .RequireRole(Settings.KeycloakAdminRoleName))
                    .AddPolicy(Settings.UserPolicyName,
                        p => p.RequireAuthenticatedUser()
                               .RequireRole(Settings.KeycloakUserRoleName))
                    .AddPolicy(Settings.AdminOrUserPolicyName,
                        p => p.RequireAuthenticatedUser()
                               .RequireRole(Settings.KeycloakAdminRoleName, Settings.KeycloakUserRoleName));

            // Register mock UnitOfWork
            services.AddScoped<IUnitOfWork>(_ => UnitOfWork);
        });

        builder.UseEnvironment("Development");
    }
}

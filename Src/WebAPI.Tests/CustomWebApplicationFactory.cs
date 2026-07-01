
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using WebAPI;

namespace WebAPI.Tests;

using Base.Persistence.Contracts;

using Persistence;

using Service;

/// <summary>
/// Hosts the WebAPI with the Service-layer (ITournamentService/ITeamService/IMatchService) replaced by
/// NSubstitute mocks, so endpoint tests exercise routing, auth, validation and status-code mapping without
/// a real database.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public ITournamentService   TournamentService   { get; } = Substitute.For<ITournamentService>();
    public ITeamService         TeamService         { get; } = Substitute.For<ITeamService>();
    public IMatchService        MatchService        { get; } = Substitute.For<IMatchService>();
    public ITransactionProvider TransactionProvider { get; } = Substitute.For<ITransactionProvider>();
    public ICurrentUserService  CurrentUserService  { get; } = Substitute.For<ICurrentUserService>();
    public TestAuthContext      TestAuth            { get; } = new();

    public CustomWebApplicationFactory()
    {
        TransactionProvider.BeginTransactionAsync().Returns(Substitute.For<ITransaction>());
    }

    /// <summary>
    /// Call at the start of each test to reset shared state (auth and received-call history).
    /// Return-value configurations carry over intentionally — each test re-configures what it needs.
    /// </summary>
    public void Reset()
    {
        TournamentService.ClearReceivedCalls();
        TeamService.ClearReceivedCalls();
        MatchService.ClearReceivedCalls();
        TransactionProvider.ClearReceivedCalls();
        CurrentUserService.ClearReceivedCalls();
        TestAuth.IsAuthenticated = true;
        TestAuth.Roles           = [];
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext/UnitOfWork/repositories, the real Service implementations, and the
            // real ICurrentUserService (which needs a live DbContext) — everything below is mocked instead.
            var persistenceAssembly = typeof(ApplicationDbContext).Assembly;
            var descriptors = services
                .Where(d => d.ServiceType == typeof(IUnitOfWork) ||
                            d.ServiceType == typeof(ITransactionProvider) ||
                            d.ServiceType == typeof(ITournamentService) ||
                            d.ServiceType == typeof(ITeamService) ||
                            d.ServiceType == typeof(IMatchService) ||
                            d.ServiceType == typeof(ICurrentUserService) ||
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

            // Register mocked services
            services.AddScoped<ITournamentService>(_ => TournamentService);
            services.AddScoped<ITeamService>(_ => TeamService);
            services.AddScoped<IMatchService>(_ => MatchService);
            services.AddScoped<ITransactionProvider>(_ => TransactionProvider);
            services.AddScoped<ICurrentUserService>(_ => CurrentUserService);
        });

        builder.UseEnvironment("Development");
    }
}

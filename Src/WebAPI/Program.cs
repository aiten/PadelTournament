using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.HttpOverrides;

using System.Threading.RateLimiting;

using Base.Persistence.Contracts;
using Base.Tools;

using FluentValidation;

using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using Keycloak.AuthServices.Common;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

using Scalar.AspNetCore;

using Persistence;

using Service;

using Shared;

using WebAPI;
using WebAPI.Endpoints;
using WebAPI.ExceptionHandlers;
using WebAPI.Hubs;
using WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration);

// Allow SignalR clients to pass the bearer token via query string (?access_token=...)
// because WebSocket connections cannot send custom headers
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var originalOnMessageReceived = options.Events?.OnMessageReceived;
    options.Events ??= new JwtBearerEvents();
    options.Events.OnMessageReceived = async context =>
    {
        if (originalOnMessageReceived != null)
            await originalOnMessageReceived(context);

        if (string.IsNullOrEmpty(context.Token))
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) &&
                context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
        }
    };
});

builder.Services.AddCors();

builder.Services
    .AddAuthorization()
    .AddKeycloakAuthorization(builder.Configuration)
    .AddAuthorizationBuilder()
    .AddPolicy(Settings.AdminPolicyName, policyBuilder =>
    {
        var keycloakOptions = builder.Configuration.GetKeycloakOptions<KeycloakAuthenticationOptions>()!;

        policyBuilder
            // .RequireRealmRoles(Settings.KeycloakAdminRoleName)                                         // Realm role is fetched from token
            // .RequireResourceRolesForClient(keycloakOptions.Resource, [Settings.KeycloakAdminRoleName]) // Require Resource Roles (for this Client)
            .RequireResourceRoles(Settings.KeycloakAdminRoleName); // Resource/Client role is fetched from token (any client)
    })
    .AddPolicy(Settings.UserPolicyName, policyBuilder =>
    {
        var keycloakOptions = builder.Configuration.GetKeycloakOptions<KeycloakAuthenticationOptions>()!;
        policyBuilder
            // .RequireRealmRoles(Settings.KeycloakUserRoleName)                                         // Realm role is fetched from token
            // .RequireResourceRolesForClient(keycloakOptions.Resource, [Settings.KeycloakUserRoleName]) // Require Resource Roles (for this Client)
            .RequireResourceRoles(Settings.KeycloakUserRoleName); // Resource/Client role is fetched from token (any client)
    })
    .AddPolicy(Settings.AdminOrUserPolicyName, policyBuilder =>
    {
        policyBuilder
            .RequireResourceRoles(Settings.KeycloakUserRoleName, Settings.KeycloakAdminRoleName);
    });

builder.Services.AddHttpContextAccessor();
var kcOptions       = builder.Configuration.GetKeycloakOptions<KeycloakAuthenticationOptions>()!;
var keycloakBaseUrl = $"{kcOptions.AuthServerUrl!.TrimEnd('/')}/realms/{kcOptions.Realm}/protocol/openid-connect";

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo { Title = "Padel Tournament", Version = "v1" };

        document.Components                 ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["oidc"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{keycloakBaseUrl}/auth"),
                    TokenUrl         = new Uri($"{keycloakBaseUrl}/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID" },
                        { "profile", "Profile" },
                    }
                }
            }
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("oidc", document), [] }
        });

        var httpContext = context.ApplicationServices.GetRequiredService<IHttpContextAccessor>().HttpContext;
        if (httpContext?.Request.Host.Value?.Contains(".cloud.htl-leonding.ac.at") == true)
        {
            var fullHostname = System.Net.Dns.GetHostEntry("").HostName;
            var hostname     = fullHostname.Split('-')[0];
            document.Servers =
            [
                new OpenApiServer { Url = $"{httpContext.Request.Scheme}s://{httpContext.Request.Host.Value}/{hostname}" }
            ];
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
    // SignalR requires AllowCredentials; AllowAnyOrigin is incompatible with it,
    // so SetIsOriginAllowed is used as the equivalent open policy.
    options.AddPolicy("SignalRCors", policy =>
        policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

builder.Services.AddSignalR();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    // Trust all proxies — nginx proxy manager IP can vary in Docker environments
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("public-lookup", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window      = TimeSpan.FromMinutes(1),
                QueueLimit  = 0,
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddSingleton<IHubNotificationService, HubNotificationService>();
builder.Services.AddScoped<ITenantContext, TenantContext>();

builder.Services
    .AddScoped<UnitOfWork>()
    .AddScoped<ITransactionProvider>(sp => sp.GetRequiredService<UnitOfWork>())
    .AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork>())
    .AddAssemblyIncludingInternals(name => name.EndsWith("Repository"), ServiceLifetime.Transient, typeof(ApplicationDbContext).Assembly)
    .AddAssemblyIncludingInternals(name => name.EndsWith("Service"),    ServiceLifetime.Transient, typeof(TournamentService).Assembly);
;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddValidatorsFromAssemblyContaining<WebAPI.Program>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.AddAuthorizationCodeFlow("oidc", flow => flow
            .WithClientId(kcOptions.Resource)
        );
    });
}

// Add CORS to support Single Page Apps (SPAs)
app.UseCors(b => b.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.UseRateLimiter();

app.UseAuthentication();

app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapGet("/api/ping", () => "pong")
    .WithName("Ping")
    .WithTags("Health");
//.RequireAuthorization(Settings.AdminPolicyName);

app.MapTournamentEndpoints("/api/tournament");
app.MapTeamEndpoints("/api/tournament");
app.MapMatchEndpoints("/api/tournament");
app.MapMatchResultEndpoints("/api/tournament");
app.MapRegistrationEndpoints("/api/registration");
app.MapPublicEndpoints("/api/public");
app.MapHub<TournamentHub>("/hubs/tournament").RequireCors("SignalRCors");
app.MapHub<PublicHub>("/hubs/public").RequireCors("SignalRCors").AllowAnonymous();

app.MapFallbackToFile("index.html");

app.Run();

namespace WebAPI
{
    public partial class Program
    {
    }
}
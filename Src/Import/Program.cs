using Base.Persistence.Contracts;
using Base.Tools;

using Import;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Persistence;
using Persistence.Model;

using Service;

using Shared;
using Shared.Exceptions;

using System;
using System.IO;
using System.Linq;

var builder = Host.CreateApplicationBuilder(args);


var configuration = builder.Configuration;

var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services
    .AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString))
    .AddScoped<UnitOfWork>()
    .AddScoped<ITransactionProvider>(sp => sp.GetRequiredService<UnitOfWork>())
    .AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork>())
    .AddAssemblyIncludingInternals(name => name.EndsWith("Repository"), ServiceLifetime.Transient, typeof(ApplicationDbContext).Assembly)
    .AddAssemblyIncludingInternals(name => name.EndsWith("Service"),    ServiceLifetime.Transient, typeof(TournamentService).Assembly);
;

builder.Services.AddScoped<ICurrentUserService, DummyCurrentUserService>();
builder.Services.AddSingleton<IHubNotificationService, DummyHubNotificationService>();
builder.Services.AddScoped<ITenantContext, DummyTenantContext>();

var host = builder.Build();

Console.WriteLine("Migrate Database");

using (var scope = host.Services.CreateScope())
{
    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    //await uow.DeleteDatabaseAsync();
    //await uow.CreateDatabaseAsync();
    await uow.MigrateDatabaseAsync();
}

Console.WriteLine("Import Data");

bool doImport = false;

if (doImport)
{
    using (var scope = host.Services.CreateScope())
    {
        var uow     = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var service = scope.ServiceProvider.GetRequiredService<ITournamentService>();
/*
        var classesCsv = await new CsvImport<ClassesCsv>().ReadAsync("ImportData/Classes2025.csv");


        await uow.Classes.AddRangeAsync(classes);
    */

        var tournaments = new[]
        {
            new Tournament()
            {
                Created         = DateTime.Now,
                Description     = "Test Tournament I",
                From            = new DateOnly(2025, 9, 18),
                To              = null,
                RegistrationPin = "12345"
            },
            new Tournament()
            {
                Created         = DateTime.Now,
                Description     = "Test Tournament II",
                From            = new DateOnly(2026, 5, 16),
                To              = new DateOnly(2026, 5, 20),
                RegistrationPin = "32100"
            }
        };
        await uow.Tournaments.AddRangeAsync(tournaments);

        await uow.SaveChangesAsync();

        for (int i = 1; i < 30; i++)
        {
            await service.RegisterTeamByPinAsync($"Team {i}", "32100");
        }

        var entries = (await File.ReadAllLinesAsync("ImportData/Bibione202509.txt"))
            .Select(raw =>
            {
                var  parts = raw.Split(';');
                var  name  = parts[0].Trim();
                int? seed  = parts.Length > 1 && int.TryParse(parts[1].Trim(), out var s1) ? s1 : null;
                int? pos   = parts.Length > 2 && int.TryParse(parts[2].Trim(), out var s2) ? s2 : null;

                return (Name: name, Seed: seed, SrateMatchPos: pos);
            })
            .ToList();


        await service.RegisterTeamsAsync(tournaments[0].Id, entries);

        await uow.SaveChangesAsync();
    }

    Console.WriteLine("done");
}
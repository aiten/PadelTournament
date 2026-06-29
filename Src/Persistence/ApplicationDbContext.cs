using Microsoft.EntityFrameworkCore;

namespace Persistence;

using Persistence.Model;

using Shared;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext currentUser) : base(options)
    {
        _currentUser = currentUser;
    }

    private ITenantContext _currentUser;

    public DbSet<Tournament> Tournaments { get; set; }
    public DbSet<Team>       Teams       { get; set; }
    public DbSet<Match>      Matches     { get; set; }
    public DbSet<Set>        Sets        { get; set; }
    public DbSet<Game>       Games       { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tournament>()
            .HasQueryFilter(c =>
                _currentUser.CanAccessAllTenants ||
                c.UserId == _currentUser.TenantId);

        modelBuilder.Entity<Team>()
            .HasQueryFilter(t =>
                _currentUser.CanAccessAllTenants ||
                t.Tournament.UserId == _currentUser.TenantId);

        modelBuilder.Entity<Match>()
            .HasQueryFilter(m =>
                _currentUser.CanAccessAllTenants ||
                m.Tournament.UserId == _currentUser.TenantId);

        modelBuilder.Entity<Set>()
            .HasQueryFilter(s =>
                _currentUser.CanAccessAllTenants ||
                s.Match.Tournament.UserId == _currentUser.TenantId);

        modelBuilder.Entity<Game>()
            .HasQueryFilter(g =>
                _currentUser.CanAccessAllTenants ||
                g.Set.Match.Tournament.UserId == _currentUser.TenantId);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
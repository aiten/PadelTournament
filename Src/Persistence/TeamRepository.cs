namespace Persistence;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Base.Persistence;
using Base.Persistence.Contracts;

using Microsoft.EntityFrameworkCore;

using Persistence.Model;

public interface ITeamRepository : IGenericRepository<Team>
{
    Task<Team?> GetByRegistrationAsync(string pin, string registrationCode);

    Task<bool> AnyPlayerAsync(int           tournamentId, string player1, string? player2);
    Task<bool> AnyRegistrationCodeAsync(int tournamentId, string code);
}

public class TeamRepository : GenericRepository<Team>, ITeamRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TeamRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Team?> GetByRegistrationAsync(string pin, string registrationCode)
    {
        return await DbSet
            .AsNoTracking()
            .Include(t => t.Tournament)
            .Where(t => t.RegistrationCode == registrationCode && t.Tournament.RegistrationPin == pin)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> AnyPlayerAsync(int tournamentId, string player1, string? player2)
    {
        return await DbSet.AnyAsync(t => t.TournamentId == tournamentId && t.Player1 == player1 && t.Player2 == player2);
    }

    public async Task<bool> AnyRegistrationCodeAsync(int tournamentId, string code)
    {
        return await DbSet.AnyAsync(t => t.TournamentId == tournamentId && t.RegistrationCode == code);
    }
}
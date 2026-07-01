namespace Persistence;

using Base.Persistence;
using Base.Persistence.Contracts;

public interface IUnitOfWork : IBaseUnitOfWork
{
    ITournamentRepository       Tournaments      { get; }
    ITeamRepository             Teams            { get; }
    IMatchRepository            Matches          { get; }
}

public class UnitOfWork : BaseUnitOfWork, IUnitOfWork
{
    public UnitOfWork(ApplicationDbContext dBContext,
        ITournamentRepository              tournaments,
        ITeamRepository                    teams,
        IMatchRepository                   matches
    ) : base(dBContext)
    {
        Tournaments       = tournaments;
        Teams             = teams;
        Matches           = matches;
    }

    public ITournamentRepository       Tournaments       { get; }
    public ITeamRepository             Teams             { get; }
    public IMatchRepository            Matches           { get; }
}
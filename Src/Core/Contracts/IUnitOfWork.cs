namespace Core.Contracts;

using Base.Core.Contracts;

public interface IUnitOfWork : IBaseUnitOfWork
{
    ITournamentRepository Tournaments { get; }
    ITeamRepository       Teams       { get; }
    IMatchRepository      Matches     { get; }
}
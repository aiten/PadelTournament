namespace Base.Persistence.Contracts;

using System;
using System.Threading.Tasks;

public interface ITransaction : IDisposable
{
    Task SaveChangesAsync();

    Task CommitTransactionAsync();
}
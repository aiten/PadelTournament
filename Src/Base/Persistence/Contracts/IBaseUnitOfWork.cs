namespace Base.Persistence.Contracts;

using System;
using System.Threading.Tasks;

public interface IBaseUnitOfWork : ITransactionProvider, IDisposable, IAsyncDisposable
{
    ITransaction BeginTransaction();

    Task<int> SaveChangesAsync();
    Task      DeleteDatabaseAsync();
    Task      MigrateDatabaseAsync();
    Task      CreateDatabaseAsync();
}
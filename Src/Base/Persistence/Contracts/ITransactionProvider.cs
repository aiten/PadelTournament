namespace Base.Persistence.Contracts;

using System;
using System.Threading.Tasks;

public interface ITransactionProvider : IDisposable, IAsyncDisposable
{
    Task<ITransaction> BeginTransactionAsync();
}
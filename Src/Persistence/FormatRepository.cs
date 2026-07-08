namespace Persistence;

using Base.Persistence;
using Base.Persistence.Contracts;

using Persistence.Model;

public interface IFormatRepository : IGenericRepository<Format>
{
}

public class FormatRepository : GenericRepository<Format>, IFormatRepository
{
    public FormatRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
}

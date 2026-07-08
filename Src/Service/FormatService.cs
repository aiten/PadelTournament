namespace Service;

using Persistence;
using Persistence.Model;

using Shared.Exceptions;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IFormatService
{
    Task<IList<Format>> GetFormatsAsync();

    Task<Format> SingleFormatAsync(int id);

    Task<Format> AddFormatAsync(Format format);

    Task UpdateFormatAsync(int id, Format format);

    Task DeleteFormatAsync(int id);
}

public class FormatService : IFormatService
{
    private readonly IUnitOfWork _uow;

    public FormatService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IList<Format>> GetFormatsAsync()
    {
        return await _uow.Formats.GetAsync();
    }

    public async Task<Format> SingleFormatAsync(int id)
    {
        return await _uow.Formats.GetByIdAsync(id) ?? throw new NotFoundException($"Format {id} not found");
    }

    public async Task<Format> AddFormatAsync(Format format)
    {
        if (format.Id != 0)
        {
            throw new IllegalValuesException("Id must be 0 for new entities");
        }

        await _uow.Formats.AddAsync(format);
        await _uow.SaveChangesAsync();

        return format;
    }

    public async Task UpdateFormatAsync(int id, Format format)
    {
        var entity = await SingleFormatAsync(id);

        entity.Name          = format.Name;
        entity.PlayingFormat = format.PlayingFormat;
        entity.BestOf        = format.BestOf;
        entity.GamesToWinSet = format.GamesToWinSet;
        entity.MinDiff       = format.MinDiff;
        entity.NoAdv         = format.NoAdv;

        await _uow.SaveChangesAsync();
    }

    public async Task DeleteFormatAsync(int id)
    {
        var entity = await SingleFormatAsync(id);

        _uow.Formats.Remove(entity);
        await _uow.SaveChangesAsync();
    }
}

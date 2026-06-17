namespace WebAPI.Services;

using System.Threading.Tasks;

public interface ICurrentUserService
{
    bool       IsAdmin { get; }
    Task<int?> GetUserIdAsync();
}
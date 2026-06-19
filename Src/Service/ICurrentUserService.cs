namespace Service;

using System.Threading.Tasks;

public interface ICurrentUserService
{
    Task<bool> IsUserAsync();
    Task<bool> IsAdminAsync();

    Task<string?> GetUserIdAsync();
}

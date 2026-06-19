namespace WebAPI.Services;

using System.Threading.Tasks;

public interface ICurrentUserService
{
    bool IsUser { get; }
    bool IsAdmin { get; }
 
    Task<int?> GetUserIdAsync();
}
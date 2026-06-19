namespace Service;

using System.Threading.Tasks;

public interface ICurrentUserService
{
    bool IsUser { get; }
    bool IsAdmin { get; }
 
    Task<string?> GetUserIdAsync();
}
namespace WebAPI.Services;

using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Persistence;

using Service;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _dbContext;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext           = dbContext;
    }

    public bool IsAdmin =>
        _httpContextAccessor.HttpContext?.User.IsInRole(Settings.KeycloakAdminRoleName) ?? false;
    public bool IsUser =>
        _httpContextAccessor.HttpContext?.User.IsInRole(Settings.KeycloakUserRoleName) ?? false;

    public async Task<string?> GetUserIdAsync()
    {
        var sub = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        // remark: asp.net automatically maps "sub" to ClaimTypes.NameIdentifier
        // 
        if (sub is null)
        {
            // not in context
            return null;
        }

        return sub;
    }
}
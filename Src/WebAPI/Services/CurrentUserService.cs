namespace WebAPI.Services;

using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Persistence;

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

    public async Task<int?> GetUserIdAsync()
    {
        var sub = _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
        if (sub is null)
            return null;
        /*
                var teacher = await _dbContext.Teachers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.KeycloakUserId == sub);

                return teacher?.Id;
        */
        return null;
    }
}
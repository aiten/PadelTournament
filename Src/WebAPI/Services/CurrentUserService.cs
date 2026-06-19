namespace WebAPI.Services;

using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using Persistence;

using Service;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor  _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;
    private readonly ApplicationDbContext  _dbContext;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IAuthorizationService authorizationService, ApplicationDbContext dbContext)
    {
        _httpContextAccessor  = httpContextAccessor;
        _authorizationService = authorizationService;
        _dbContext            = dbContext;
    }

    public async Task<bool> IsAdminAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;
        var result = await _authorizationService.AuthorizeAsync(user, null, Settings.AdminPolicyName);
        return result.Succeeded;
    }

    public async Task<bool> IsUserAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;
        var result = await _authorizationService.AuthorizeAsync(user, null, Settings.UserPolicyName);
        return result.Succeeded;
    }

    public Task<string?> GetUserIdAsync()
    {
        // asp.net automatically maps "sub" to ClaimTypes.NameIdentifier
        var sub = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Task.FromResult(sub);
    }
}

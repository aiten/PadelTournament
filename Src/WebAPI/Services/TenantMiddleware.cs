namespace WebAPI.Services;

using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Service;

using Shared;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext    context,
        ITenantContext tenantContext,
        ICurrentUserService   userService)
    {
        // 1. Tenant aus JWT
        var tenantId = await userService.GetUserIdAsync(); ;

        // 2. Async Rollen-/Rechteprüfung
        var isGlobalAdmin = await userService.IsAdminAsync();

        // 3. Kontext setzen
        tenantContext.Initialize(tenantId, isGlobalAdmin);

        // 4. Weiter zum nächsten Schritt
        await _next(context);
    }
}
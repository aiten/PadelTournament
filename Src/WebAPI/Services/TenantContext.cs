namespace WebAPI.Services;

using Microsoft.AspNetCore.Http;

using Service;

using System;
using System.Threading.Tasks;

using Shared;

public class TenantContext : ITenantContext
{
    public string? TenantId            { get; private set; }
    public bool    CanAccessAllTenants { get; private set; }

    public void Initialize(string? tenantId, bool canAccessAllTenants)
    {
        TenantId            = tenantId;
        CanAccessAllTenants = canAccessAllTenants;
    }
}
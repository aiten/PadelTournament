using Shared;

namespace Import;

public class DummyTenantContext : ITenantContext
{
    public string? TenantId            { get; private set; }
    public bool    CanAccessAllTenants { get; private set; }

    public void Initialize(string? tenantId, bool canAccessAllTenants)
    {
        TenantId            = tenantId;
        CanAccessAllTenants = canAccessAllTenants;
    }
}
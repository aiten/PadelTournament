namespace Shared;

public interface ITenantContext
{
    string? TenantId            { get; }
    bool    CanAccessAllTenants { get; }

    void Initialize(string? tenantId, bool canAccessAllTenants);
}
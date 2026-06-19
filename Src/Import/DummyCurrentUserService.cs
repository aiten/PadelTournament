namespace Import;

using System.Threading.Tasks;

using Service;

public class DummyCurrentUserService : ICurrentUserService
{
    public Task<bool> IsAdminAsync() => Task.FromResult(true);
    public Task<bool> IsUserAsync()  => Task.FromResult(true);

    public Task<string?> GetUserIdAsync() => Task.FromResult<string?>("user@xyz.com");
}

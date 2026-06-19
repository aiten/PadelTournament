namespace Import;

using System.Threading.Tasks;

using Service;

public class DummyCurrentUserService : ICurrentUserService
{

    public bool IsAdmin => true;
    public bool IsUser  => true;

    public async Task<string?> GetUserIdAsync() => "user@xyz.com";
}
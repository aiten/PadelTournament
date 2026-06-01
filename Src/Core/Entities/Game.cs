namespace Core.Entities;

using Base.Core.Entities;

public class Game : EntityObject
{
    public int    No       { get; set; }
    public string Points   { get; set; } = null!;

    public Server? Server { get; set; }

    public Set Set   { get; set; } = null!;
    public int SetId { get; set; }
}

namespace Base.Core.Entities;

using Base.Core.Contracts;

public class EntityObject : IEntityObject
{
    public int Id { get; set; }

    public byte[]? RowVersion { get; set; }
}
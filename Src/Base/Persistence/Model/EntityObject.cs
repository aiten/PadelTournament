namespace Base.Persistence.Model;

using System.ComponentModel.DataAnnotations;

using Base.Persistence.Contracts;

public class EntityObject : IEntityObject
{
    [Key]
    public int Id { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
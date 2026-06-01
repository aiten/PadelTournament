namespace Core.Entities;

using System;

using Base.Core.Entities;

public class Team : EntityObject
{
    public required string  Player1 { get; set; }
    public          string? Player2 { get; set; }

    public string Name => Player2 is null ? Player1 : $"{Player1}/{Player2}";

    public DateTime RegistrationDate { get; set; }

    public int?    Seed             { get; set; }
    public int?    StartMatchPos    { get; set; }
    public string? RegistrationCode { get; set; }

    public Tournament Tournament   { get; set; } = null!;
    public int        TournamentId { get; set; }
}
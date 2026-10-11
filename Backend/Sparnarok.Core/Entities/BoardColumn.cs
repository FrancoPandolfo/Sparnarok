using System;
using Sparnarok.Core.Interfaces;

namespace Sparnarok.Core.Entities;

public class BoardColumn : IMustHaveParty
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

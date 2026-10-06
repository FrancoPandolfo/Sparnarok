using System;

namespace Sparnarok.Core.Entities;

public class PartyMember
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Party? Party { get; set; }
    public User? User { get; set; }
}

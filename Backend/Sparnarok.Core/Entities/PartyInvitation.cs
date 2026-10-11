using System;

namespace Sparnarok.Core.Entities;

public class PartyInvitation
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public Guid InviterId { get; set; }
    public string InviteeEmail { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Declined
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Party? Party { get; set; }
    public User? Inviter { get; set; }
}

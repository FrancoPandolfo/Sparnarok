using System;
using System.Collections.Generic;

namespace Sparnarok.Core.Entities;

public class Party
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPremium { get; set; }
    public Dictionary<string, decimal> TagMultipliers { get; set; } = new();
    public string WebhookSecret { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PartyMember> Members { get; set; } = new List<PartyMember>();
}

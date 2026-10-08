using System;

namespace Sparnarok.Core.Entities;

public class PartyWebhookEvent
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadPreview { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}

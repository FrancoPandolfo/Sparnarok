using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Sparnarok.Api.Hubs;
using Sparnarok.Application.Interfaces;

namespace Sparnarok.Api.Services;

public class SignalRNotificationService : IRealTimeNotificationService
{
    private readonly IHubContext<PartyHub> _hubContext;

    public SignalRNotificationService(IHubContext<PartyHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyQuestUpdatedAsync(Guid partyId, Guid questId, string newState, Guid sourceUserId)
    {
        await _hubContext.Clients.Group($"party-{partyId}").SendAsync("QuestUpdated", new 
        { 
            QuestId = questId, 
            NewState = newState, 
            SourceUserId = sourceUserId 
        });
    }

    public async Task NotifyNewActivityAsync(Guid partyId, string message, int xpGained)
    {
        await _hubContext.Clients.Group($"party-{partyId}").SendAsync("NewPartyActivity", new 
        {
            Id = Guid.NewGuid(),
            Message = message,
            Xp = xpGained,
            Timestamp = DateTime.UtcNow
        });
    }
}

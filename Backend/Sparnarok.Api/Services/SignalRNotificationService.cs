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
        var groupName = $"party-{partyId.ToString().ToLower()}";
        await _hubContext.Clients.Group(groupName).SendAsync("QuestUpdated", new 
        { 
            questId = questId.ToString(), 
            newState = newState, 
            sourceUserId = sourceUserId.ToString() 
        });
    }

    public async Task NotifyNewActivityAsync(Guid partyId, string message, int xpGained, Guid? sourceUserId = null, string? username = null, Guid? questId = null)
    {
        var groupName = $"party-{partyId.ToString().ToLower()}";
        await _hubContext.Clients.Group(groupName).SendAsync("NewPartyActivity", new 
        {
            id = Guid.NewGuid().ToString(),
            message = message,
            xp = xpGained,
            sourceUserId = sourceUserId?.ToString(),
            username = username,
            questId = questId?.ToString(),
            timestamp = DateTime.UtcNow
        });
    }
}

using System;
using System.Threading.Tasks;

namespace Sparnarok.Application.Interfaces;

public interface IRealTimeNotificationService
{
    Task NotifyQuestUpdatedAsync(Guid partyId, Guid questId, string newState, Guid sourceUserId);
    Task NotifyNewActivityAsync(Guid partyId, string message, int xpGained, Guid? sourceUserId = null, string? username = null, Guid? questId = null);
}

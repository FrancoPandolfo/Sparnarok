using System;
using System.Threading.Tasks;
using Sparnarok.Core.Entities;
using Sparnarok.Application.DTOs;
using Sparnarok.Core.Enums;

namespace Sparnarok.Application.Interfaces;

public interface IQuestService
{
    Task<Quest> CreateQuestAsync(CreateQuestDto dto);
    Task<Quest> UpdateQuestStatusAsync(Guid questId, QuestState newState, Guid userId, string? username = null);
    Task<Quest> AssignQuestAsync(Guid questId, Guid? assigneeId);
    Task<System.Collections.Generic.IEnumerable<Quest>> GetQuestsByPartyAsync(Guid partyId);
}


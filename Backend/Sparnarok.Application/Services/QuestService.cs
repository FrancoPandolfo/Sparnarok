using System;
using System.Linq;
using System.Threading.Tasks;
using Sparnarok.Core.Entities;
using Sparnarok.Core.Enums;
using Sparnarok.Application.Interfaces;
using Sparnarok.Application.DTOs;
using Sparnarok.Infrastructure.Data;

namespace Sparnarok.Application.Services;

public class QuestService : IQuestService
{
    private readonly SparnarokDbContext _context;

    public QuestService(SparnarokDbContext context)
    {
        _context = context;
    }

    public async Task<Quest> CreateQuestAsync(CreateQuestDto dto)
    {
        var quest = new Quest
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Difficulty = dto.Difficulty,
            State = QuestState.Pending,
            CreatedAt = DateTime.UtcNow,
            Rewards = dto.Rewards.Select(r => new QuestReward
            {
                Id = Guid.NewGuid(),
                SkillCategoryId = r.SkillCategoryId,
                XpAmount = r.XpAmount
            }).ToList()
        };

        _context.Quests.Add(quest);
        
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Fallo al inscribir la Quest en los registros de Sparnarok.", ex);
        }

        return quest;
    }
}

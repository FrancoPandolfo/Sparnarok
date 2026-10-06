using System;
using System.Linq;
using System.Threading.Tasks;
using Sparnarok.Core.Entities;
using Sparnarok.Core.Enums;
using Sparnarok.Application.Interfaces;
using Sparnarok.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Sparnarok.Application.Services;

public class QuestService : IQuestService
{
    private readonly ISparnarokDbContext _context;

    public QuestService(ISparnarokDbContext context)
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

    public async Task<Quest> UpdateQuestStatusAsync(Guid questId, QuestState newState, Guid userId)
    {
        var quest = await _context.Quests
            .Include(q => q.Rewards)
            .FirstOrDefaultAsync(q => q.Id == questId);

        if (quest == null)
            throw new ApplicationException("Quest no encontrada en los registros.");

        if (quest.State == QuestState.Completed)
            throw new ApplicationException("La Quest ya ha sido forjada y completada.");

        quest.State = newState;

        if (newState == QuestState.Completed)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                var xpGained = quest.Rewards.Sum(r => r.XpAmount);
                user.TotalXp += xpGained;

                foreach (var reward in quest.Rewards)
                {
                    var progression = await _context.UserSkillProgressions
                        .FirstOrDefaultAsync(p => p.UserId == userId && p.SkillCategoryId == reward.SkillCategoryId);
                    
                    if (progression == null)
                    {
                        progression = new UserSkillProgression
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            SkillCategoryId = reward.SkillCategoryId,
                            CurrentXp = 0,
                            Level = 1
                        };
                        _context.UserSkillProgressions.Add(progression);
                    }
                    
                    progression.CurrentXp += reward.XpAmount;
                    progression.Level = 1 + (progression.CurrentXp / 100);
                }
            }
        }

        await _context.SaveChangesAsync();
        return quest;
    }
}

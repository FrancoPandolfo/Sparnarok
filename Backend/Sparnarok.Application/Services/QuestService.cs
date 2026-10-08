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
    private readonly IRealTimeNotificationService _notificationService;

    public QuestService(ISparnarokDbContext context, IRealTimeNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Quest> CreateQuestAsync(CreateQuestDto dto)
    {
        var questId = Guid.NewGuid();
        var quest = new Quest
        {
            Id = questId,
            DisplayId = "SPAR-" + questId.ToString().Substring(0, 8).ToUpper(),
            Title = dto.Title,
            Description = dto.Description,
            Difficulty = dto.Difficulty,
            State = QuestState.Pending,
            CreatedAt = DateTime.UtcNow,
            Tags = dto.Tags ?? new System.Collections.Generic.List<string>(),
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

        Sparnarok.Core.Entities.User? user = null;
        decimal totalMultiplier = 1.0m;

        if (newState == QuestState.Completed)
        {
            user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            // Calculate XP Multiplier based on Party tags
            var party = await _context.Parties.FirstOrDefaultAsync(p => p.Id == quest.PartyId);
            var multipliers = party?.TagMultipliers ?? new System.Collections.Generic.Dictionary<string, decimal>();
            if (quest.Tags != null)
            {
                foreach(var tag in quest.Tags)
                {
                    if (multipliers.TryGetValue(tag, out var mult))
                    {
                        totalMultiplier *= mult;
                    }
                }
            }

            if (user != null)
            {
                var baseXP = quest.Rewards.Sum(r => r.XpAmount);
                var xpGained = (int)Math.Round(baseXP * totalMultiplier, MidpointRounding.AwayFromZero);
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
                            PartyId = quest.PartyId,
                            UserId = userId,
                            SkillCategoryId = reward.SkillCategoryId,
                            CurrentXp = 0,
                            Level = 1
                        };
                        _context.UserSkillProgressions.Add(progression);
                    }
                    
                    var rewardXp = (int)Math.Round(reward.XpAmount * totalMultiplier, MidpointRounding.AwayFromZero);
                    progression.CurrentXp += rewardXp;
                    progression.Level = 1 + (progression.CurrentXp / 100);
                }
            }
        }

        await _context.SaveChangesAsync();

        await _notificationService.NotifyQuestUpdatedAsync(quest.PartyId, quest.Id, newState.ToString(), userId);

        if (newState == QuestState.Completed && user != null)
        {
            var totalXpGained = (int)Math.Round(quest.Rewards.Sum(r => r.XpAmount) * totalMultiplier, MidpointRounding.AwayFromZero);
            await _notificationService.NotifyNewActivityAsync(quest.PartyId, $"{user.Username} completó '{quest.Title}'", totalXpGained);
        }

        return quest;
    }
}

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
    private readonly ITenantService? _tenantService;

    public QuestService(
        ISparnarokDbContext context, 
        IRealTimeNotificationService notificationService,
        ITenantService? tenantService = null)
    {
        _context = context;
        _notificationService = notificationService;
        _tenantService = tenantService;
    }

    public async Task<Quest> CreateQuestAsync(CreateQuestDto dto)
    {
        var questId = Guid.NewGuid();
        var partyId = _tenantService?.GetCurrentPartyId() ?? Guid.Empty;
        var quest = new Quest
        {
            Id = questId,
            PartyId = partyId,
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
            .IgnoreQueryFilters()
            .Include(q => q.Rewards)
            .FirstOrDefaultAsync(q => q.Id == questId);

        if (quest == null)
            throw new ApplicationException("Quest no encontrada en los registros.");

        var wasAlreadyCompleted = quest.State == QuestState.Completed;
        quest.State = newState;

        if (quest.PartyId == Guid.Empty)
        {
            var fallbackTenantId = _tenantService?.GetCurrentPartyId() ?? Guid.Empty;
            if (fallbackTenantId != Guid.Empty)
            {
                quest.PartyId = fallbackTenantId;
            }
        }

        Sparnarok.Core.Entities.User? user = null;
        decimal totalMultiplier = 1.0m;

        if (newState == QuestState.Completed && !wasAlreadyCompleted)
        {
            user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
            
            // Calculate XP Multiplier based on Party tags
            var party = await _context.Parties.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == quest.PartyId);
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
                        .IgnoreQueryFilters()
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

        if (newState == QuestState.Completed && !wasAlreadyCompleted)
        {
            var heroName = user?.Username ?? "Un aliado";
            var baseRewardXp = quest.Rewards != null && quest.Rewards.Any() ? quest.Rewards.Sum(r => r.XpAmount) : 100;
            var totalXpGained = (int)Math.Round(baseRewardXp * totalMultiplier, MidpointRounding.AwayFromZero);
            if (totalXpGained <= 0) totalXpGained = 100;
            await _notificationService.NotifyNewActivityAsync(quest.PartyId, $"{heroName} completó '{quest.Title}'", totalXpGained);
        }

        return quest;
    }

    public async Task<System.Collections.Generic.IEnumerable<Quest>> GetQuestsByPartyAsync(Guid partyId)
    {
        var partyExists = await _context.Parties.IgnoreQueryFilters().AnyAsync(p => p.Id == partyId);
        if (!partyExists)
        {
            return new List<Quest>();
        }

        var quests = await _context.Quests
            .IgnoreQueryFilters()
            .Include(q => q.Rewards)
            .Where(q => q.PartyId == partyId)
            .OrderBy(q => q.CreatedAt)
            .ToListAsync();

        if (quests.Count == 0)
        {
            var firstCategory = await _context.SkillCategories.FirstOrDefaultAsync();
            if (firstCategory == null)
            {
                firstCategory = new SkillCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "Frontend",
                    Description = "Desarrollo Frontend y UI",
                    IsPremiumTier = false
                };
                var backendCat = new SkillCategory
                {
                    Id = Guid.NewGuid(),
                    Name = "Backend",
                    Description = "Desarrollo Backend y APIs",
                    IsPremiumTier = false
                };
                _context.SkillCategories.AddRange(firstCategory, backendCat);
                await _context.SaveChangesAsync();
            }
            var skillCatId = firstCategory.Id;

            var q1 = new Quest
            {
                Id = Guid.NewGuid(),
                PartyId = partyId,
                DisplayId = "SPAR-BUG-LOGIN",
                Title = "Derrotar al Bug del Login",
                Description = "El login devuelve 500 a veces.",
                Difficulty = QuestDifficulty.D,
                State = QuestState.Pending,
                Tags = new System.Collections.Generic.List<string> { "Urgente", "Bug" },
                Rewards = new System.Collections.Generic.List<QuestReward>
                {
                    new QuestReward { Id = Guid.NewGuid(), SkillCategoryId = skillCatId, XpAmount = 50 }
                },
                CreatedAt = DateTime.UtcNow
            };

            var q2 = new Quest
            {
                Id = Guid.NewGuid(),
                PartyId = partyId,
                DisplayId = "SPAR-AUTH-MW",
                Title = "Refactorizar Auth a Middleware",
                Description = "Mejorar seguridad.",
                Difficulty = QuestDifficulty.B,
                State = QuestState.InProgress,
                Tags = new System.Collections.Generic.List<string>(),
                Rewards = new System.Collections.Generic.List<QuestReward>
                {
                    new QuestReward { Id = Guid.NewGuid(), SkillCategoryId = skillCatId, XpAmount = 120 }
                },
                CreatedAt = DateTime.UtcNow.AddMinutes(1)
            };

            _context.Quests.AddRange(q1, q2);
            await _context.SaveChangesAsync();
            quests.Add(q1);
            quests.Add(q2);
        }

        return quests;
    }
}

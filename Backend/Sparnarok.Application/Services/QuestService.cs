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
            AssigneeId = (dto.AssigneeId.HasValue && dto.AssigneeId.Value != Guid.Empty) ? dto.AssigneeId : null,
            CreatedAt = DateTime.UtcNow,
            Tags = dto.Tags ?? new System.Collections.Generic.List<string>(),
            Rewards = dto.Rewards.Select(r => new QuestReward
            {
                Id = Guid.NewGuid(),
                SkillCategoryId = r.SkillCategoryId,
                XpAmount = r.XpAmount
            }).ToList()
        };

        if (quest.AssigneeId.HasValue)
        {
            quest.Assignee = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == quest.AssigneeId.Value);
        }

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

    public async Task<Quest> AssignQuestAsync(Guid questId, Guid? assigneeId)
    {
        var quest = await _context.Quests
            .IgnoreQueryFilters()
            .Include(q => q.Rewards)
            .Include(q => q.Assignee)
            .FirstOrDefaultAsync(q => q.Id == questId);

        if (quest == null)
            throw new ApplicationException("Quest no encontrada en los registros.");

        if (assigneeId.HasValue && assigneeId.Value != Guid.Empty)
        {
            var userExists = await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == assigneeId.Value);
            if (!userExists)
                throw new ApplicationException("El héroe asignado no existe.");
            quest.AssigneeId = assigneeId.Value;
            quest.Assignee = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == assigneeId.Value);
        }
        else
        {
            quest.AssigneeId = null;
            quest.Assignee = null;
        }

        await _context.SaveChangesAsync();

        await _notificationService.NotifyQuestUpdatedAsync(quest.PartyId, quest.Id, quest.State.ToString(), quest.AssigneeId ?? Guid.Empty);

        return quest;
    }

    public async Task<Quest> UpdateQuestStatusAsync(Guid questId, QuestState newState, Guid userId, string? requestUsername = null)
    {
        var quest = await _context.Quests
            .IgnoreQueryFilters()
            .Include(q => q.Rewards)
            .Include(q => q.Assignee)
            .FirstOrDefaultAsync(q => q.Id == questId);

        if (quest == null)
            throw new ApplicationException("Quest no encontrada en los registros.");

        var oldState = quest.State;
        quest.State = newState;

        if (quest.PartyId == Guid.Empty)
        {
            var fallbackTenantId = _tenantService?.GetCurrentPartyId() ?? Guid.Empty;
            if (fallbackTenantId != Guid.Empty)
            {
                quest.PartyId = fallbackTenantId;
            }
        }

        // The user who receives or loses XP is quest.AssigneeId. Fallback to userId if not assigned.
        var targetUserId = (quest.AssigneeId.HasValue && quest.AssigneeId.Value != Guid.Empty)
            ? quest.AssigneeId.Value
            : userId;

        Sparnarok.Core.Entities.User? user = null;
        decimal totalMultiplier = 1.0m;

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

        if (newState == QuestState.Completed && oldState != QuestState.Completed)
        {
            user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == targetUserId);
            if (user != null)
            {
                var baseXP = quest.Rewards.Sum(r => r.XpAmount);
                var xpGained = (int)Math.Round(baseXP * totalMultiplier, MidpointRounding.AwayFromZero);
                user.TotalXp += xpGained;

                foreach (var reward in quest.Rewards)
                {
                    var progression = await _context.UserSkillProgressions
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.UserId == targetUserId && p.SkillCategoryId == reward.SkillCategoryId);
                    
                    if (progression == null)
                    {
                        progression = new UserSkillProgression
                        {
                            Id = Guid.NewGuid(),
                            PartyId = quest.PartyId,
                            UserId = targetUserId,
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
        else if (oldState == QuestState.Completed && newState != QuestState.Completed)
        {
            user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == targetUserId);
            if (user != null)
            {
                var baseXP = quest.Rewards.Sum(r => r.XpAmount);
                var xpRollback = (int)Math.Round(baseXP * totalMultiplier, MidpointRounding.AwayFromZero);
                user.TotalXp = Math.Max(0, user.TotalXp - xpRollback);

                foreach (var reward in quest.Rewards)
                {
                    var progression = await _context.UserSkillProgressions
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.UserId == targetUserId && p.SkillCategoryId == reward.SkillCategoryId);

                    if (progression != null)
                    {
                        var rewardXp = (int)Math.Round(reward.XpAmount * totalMultiplier, MidpointRounding.AwayFromZero);
                        progression.CurrentXp = Math.Max(0, progression.CurrentXp - rewardXp);
                        progression.Level = Math.Max(1, 1 + (progression.CurrentXp / 100));
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        await _notificationService.NotifyQuestUpdatedAsync(quest.PartyId, quest.Id, newState.ToString(), userId);

        if (newState == QuestState.Completed && oldState != QuestState.Completed)
        {
            var heroName = quest.Assignee?.Username 
                ?? user?.Username 
                ?? (!string.IsNullOrWhiteSpace(requestUsername) && targetUserId == userId ? requestUsername : "Aliado");
            var baseRewardXp = quest.Rewards != null && quest.Rewards.Any() ? quest.Rewards.Sum(r => r.XpAmount) : 100;
            var totalXpGained = (int)Math.Round(baseRewardXp * totalMultiplier, MidpointRounding.AwayFromZero);
            if (totalXpGained <= 0) totalXpGained = 100;
            await _notificationService.NotifyNewActivityAsync(
                quest.PartyId, 
                $"El aliado {heroName} completó '{quest.Title}'", 
                totalXpGained,
                sourceUserId: user?.Id ?? (targetUserId != Guid.Empty ? targetUserId : null),
                username: heroName,
                questId: quest.Id
            );
        }
        else if (oldState == QuestState.Completed && newState != QuestState.Completed)
        {
            var heroName = quest.Assignee?.Username 
                ?? user?.Username 
                ?? (!string.IsNullOrWhiteSpace(requestUsername) && targetUserId == userId ? requestUsername : "Aliado");
            var baseRewardXp = quest.Rewards != null && quest.Rewards.Any() ? quest.Rewards.Sum(r => r.XpAmount) : 100;
            var totalXpRollback = (int)Math.Round(baseRewardXp * totalMultiplier, MidpointRounding.AwayFromZero);
            if (totalXpRollback <= 0) totalXpRollback = 100;
            await _notificationService.NotifyNewActivityAsync(
                quest.PartyId, 
                $"{heroName} ha devuelto '{quest.Title}' a desarrollo", 
                -totalXpRollback,
                sourceUserId: user?.Id ?? (targetUserId != Guid.Empty ? targetUserId : null),
                username: heroName,
                questId: quest.Id
            );
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
            .Include(q => q.Assignee)
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Application.DTOs;
using Sparnarok.Application.Interfaces;
using Sparnarok.Application.Services;
using Sparnarok.Core.Enums;
using Sparnarok.Infrastructure.Data;
using Xunit;

namespace Sparnarok.Tests.Unit.Services;

public class QuestServiceTests
{
    private class FakeTenantService : ITenantService
    {
        public Guid GetCurrentPartyId() => Guid.Empty;
        public void SetCurrentPartyId(Guid partyId) {}
    }

    private SparnarokDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<SparnarokDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SparnarokDbContext(options, new FakeTenantService());
    }

    [Fact]
    public async Task CreateQuestAsync_ShouldCreateQuestWithRewards()
    {
        // Arrange
        using var context = GetDbContext();
        var service = new QuestService(context);
        
        var dto = new CreateQuestDto
        {
            Title = "Refactorizar Auth",
            Description = "Mover lógica a un middleware",
            Difficulty = QuestDifficulty.B,
            Rewards = new List<QuestRewardDto>
            {
                new QuestRewardDto { SkillCategoryId = Guid.NewGuid(), XpAmount = 100 }
            }
        };

        // Act
        var result = await service.CreateQuestAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(dto.Title);
        result.State.Should().Be(QuestState.Pending);
        result.Rewards.Should().HaveCount(1);
        result.Rewards.First().XpAmount.Should().Be(100);

        var savedQuest = await context.Quests.Include(q => q.Rewards).FirstOrDefaultAsync(q => q.Id == result.Id);
        savedQuest.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateQuestStatusAsync_ToCompleted_ShouldAddXpToUser()
    {
        // Arrange
        using var context = GetDbContext();
        var service = new QuestService(context);
        
        var userId = Guid.NewGuid();
        var user = new Sparnarok.Core.Entities.User { Id = userId, Username = "Hero", TotalXp = 10 };
        context.Users.Add(user);

        var quest = new Sparnarok.Core.Entities.Quest
        {
            Id = Guid.NewGuid(),
            Title = "Defeat Bug",
            State = QuestState.InProgress,
            Rewards = new List<Sparnarok.Core.Entities.QuestReward>
            {
                new Sparnarok.Core.Entities.QuestReward { SkillCategoryId = Guid.NewGuid(), XpAmount = 50 }
            }
        };
        context.Quests.Add(quest);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateQuestStatusAsync(quest.Id, QuestState.Completed, userId);

        // Assert
        result.State.Should().Be(QuestState.Completed);
        var updatedUser = await context.Users.FindAsync(userId);
        updatedUser!.TotalXp.Should().Be(60); // 10 base + 50 reward

        var progression = await context.UserSkillProgressions.FirstOrDefaultAsync(p => p.UserId == userId);
        progression.Should().NotBeNull();
        progression!.CurrentXp.Should().Be(50);
        progression.Level.Should().Be(1); // 1 + (50/100) = 1
    }

    [Fact]
    public async Task UpdateQuestStatusAsync_ToCompleted_ShouldLevelUpUserSkillWhenXpExceeds100()
    {
        // Arrange
        using var context = GetDbContext();
        var service = new QuestService(context);
        
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        context.Users.Add(new Sparnarok.Core.Entities.User { Id = userId, Username = "Hero", TotalXp = 0 });
        context.UserSkillProgressions.Add(new Sparnarok.Core.Entities.UserSkillProgression
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SkillCategoryId = skillId,
            CurrentXp = 80,
            Level = 1
        });

        var quest = new Sparnarok.Core.Entities.Quest
        {
            Id = Guid.NewGuid(),
            Title = "Big Boss",
            State = QuestState.InProgress,
            Rewards = new List<Sparnarok.Core.Entities.QuestReward>
            {
                new Sparnarok.Core.Entities.QuestReward { SkillCategoryId = skillId, XpAmount = 50 }
            }
        };
        context.Quests.Add(quest);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateQuestStatusAsync(quest.Id, QuestState.Completed, userId);

        // Assert
        var progression = await context.UserSkillProgressions.FirstOrDefaultAsync(p => p.UserId == userId);
        progression!.CurrentXp.Should().Be(130);
        progression.Level.Should().Be(2); // 1 + (130/100) = 2
    }

    [Fact]
    public async Task UpdateQuestStatusAsync_WithTagMultiplier_ShouldApplyMultiplierToXp()
    {
        // Arrange
        using var context = GetDbContext();
        var service = new QuestService(context);
        
        var userId = Guid.NewGuid();
        var partyId = Guid.Empty;
        var skillId = Guid.NewGuid();
        
        var user = new Sparnarok.Core.Entities.User { Id = userId, Username = "Hero", TotalXp = 0 };
        context.Users.Add(user);

        var party = new Sparnarok.Core.Entities.Party 
        { 
            Id = partyId, 
            TagMultipliers = new Dictionary<string, decimal> { { "Bug", 1.5m } } 
        };
        context.Parties.Add(party);

        var quest = new Sparnarok.Core.Entities.Quest
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            Title = "Fix critical bug",
            State = QuestState.InProgress,
            Tags = new List<string> { "Bug" },
            Rewards = new List<Sparnarok.Core.Entities.QuestReward>
            {
                new Sparnarok.Core.Entities.QuestReward { SkillCategoryId = skillId, XpAmount = 100 }
            }
        };
        context.Quests.Add(quest);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateQuestStatusAsync(quest.Id, QuestState.Completed, userId);

        // Assert
        var updatedUser = await context.Users.FindAsync(userId);
        updatedUser!.TotalXp.Should().Be(150); // 100 * 1.5 = 150

        var progression = await context.UserSkillProgressions.FirstOrDefaultAsync(p => p.UserId == userId);
        progression!.CurrentXp.Should().Be(150);
    }
}

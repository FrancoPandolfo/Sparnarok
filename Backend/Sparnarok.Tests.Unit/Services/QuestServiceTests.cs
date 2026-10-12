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
        public Guid CurrentId { get; set; } = Guid.Empty;
        public Guid GetCurrentPartyId() => CurrentId;
        public void SetCurrentPartyId(Guid partyId) { CurrentId = partyId; }
    }

    private SparnarokDbContext GetDbContext(Guid? tenantId = null)
    {
        var options = new DbContextOptionsBuilder<SparnarokDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var tenant = new FakeTenantService();
        if (tenantId.HasValue) tenant.CurrentId = tenantId.Value;
        return new SparnarokDbContext(options, tenant);
    }

    [Fact]
    public async Task CreateQuestAsync_ShouldCreateQuestWithRewards()
    {
        // Arrange
        using var context = GetDbContext();
        var mockNotificationService = new Moq.Mock<IRealTimeNotificationService>();
        var service = new QuestService(context, mockNotificationService.Object);
        
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
        var mockNotificationService = new Moq.Mock<IRealTimeNotificationService>();
        var service = new QuestService(context, mockNotificationService.Object);
        
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
        var mockNotificationService = new Moq.Mock<IRealTimeNotificationService>();
        var service = new QuestService(context, mockNotificationService.Object);
        
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
        var partyId = Guid.NewGuid();
        using var context = GetDbContext(partyId);
        var mockNotificationService = new Moq.Mock<IRealTimeNotificationService>();
        var service = new QuestService(context, mockNotificationService.Object);
        
        var userId = Guid.NewGuid();
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

    [Fact]
    public async Task UpdateQuestStatusAsync_FromCompletedToInProgress_ShouldRollbackUserXpAndSkillProgression()
    {
        // Arrange
        var partyId = Guid.NewGuid();
        using var context = GetDbContext(partyId);
        var mockNotificationService = new Moq.Mock<IRealTimeNotificationService>();
        var service = new QuestService(context, mockNotificationService.Object);
        
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        context.Users.Add(new Sparnarok.Core.Entities.User { Id = userId, Username = "Hero", TotalXp = 150 });
        context.UserSkillProgressions.Add(new Sparnarok.Core.Entities.UserSkillProgression
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            UserId = userId,
            SkillCategoryId = skillId,
            CurrentXp = 150,
            Level = 2
        });

        var quest = new Sparnarok.Core.Entities.Quest
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            Title = "Ticket Rejected",
            State = QuestState.Completed,
            Rewards = new List<Sparnarok.Core.Entities.QuestReward>
            {
                new Sparnarok.Core.Entities.QuestReward { SkillCategoryId = skillId, XpAmount = 100 }
            }
        };
        context.Quests.Add(quest);
        await context.SaveChangesAsync();

        // Act - Moving from Completed to InProgress (rollback)
        var result = await service.UpdateQuestStatusAsync(quest.Id, QuestState.InProgress, userId);

        // Assert
        result.State.Should().Be(QuestState.InProgress);
        var updatedUser = await context.Users.FindAsync(userId);
        updatedUser!.TotalXp.Should().Be(50); // 150 - 100

        var progression = await context.UserSkillProgressions.FirstOrDefaultAsync(p => p.UserId == userId);
        progression!.CurrentXp.Should().Be(50);
        progression.Level.Should().Be(1); // 1 + (50/100) = 1

        mockNotificationService.Verify(n => n.NotifyNewActivityAsync(
            partyId,
            Moq.It.Is<string>(msg => msg.Contains("ha devuelto 'Ticket Rejected' a desarrollo")),
            -100,
            userId,
            "Hero",
            quest.Id
        ), Moq.Times.Once);
    }

    [Fact]
    public async Task UpdateQuestStatusAsync_Rollback_ShouldNeverDropXpBelowZero()
    {
        // Arrange
        var partyId = Guid.NewGuid();
        using var context = GetDbContext(partyId);
        var mockNotificationService = new Moq.Mock<IRealTimeNotificationService>();
        var service = new QuestService(context, mockNotificationService.Object);
        
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        context.Users.Add(new Sparnarok.Core.Entities.User { Id = userId, Username = "Hero", TotalXp = 30 });
        context.UserSkillProgressions.Add(new Sparnarok.Core.Entities.UserSkillProgression
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            UserId = userId,
            SkillCategoryId = skillId,
            CurrentXp = 30,
            Level = 1
        });

        var quest = new Sparnarok.Core.Entities.Quest
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            Title = "Overkill Rollback",
            State = QuestState.Completed,
            Rewards = new List<Sparnarok.Core.Entities.QuestReward>
            {
                new Sparnarok.Core.Entities.QuestReward { SkillCategoryId = skillId, XpAmount = 100 }
            }
        };
        context.Quests.Add(quest);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateQuestStatusAsync(quest.Id, QuestState.Pending, userId);

        // Assert
        var updatedUser = await context.Users.FindAsync(userId);
        updatedUser!.TotalXp.Should().Be(0); // Clamped at 0

        var progression = await context.UserSkillProgressions.FirstOrDefaultAsync(p => p.UserId == userId);
        progression!.CurrentXp.Should().Be(0); // Clamped at 0
        progression.Level.Should().Be(1); // Clamped at 1
    }

    [Fact]
    public async Task UpdateQuestStatusAsync_ToCompleted_WhenAssigned_ShouldAwardXpToAssignee_NotCardMover()
    {
        // Arrange
        var partyId = Guid.NewGuid();
        using var context = GetDbContext(partyId);
        var mockNotificationService = new Moq.Mock<IRealTimeNotificationService>();
        var service = new QuestService(context, mockNotificationService.Object);

        var moverId = Guid.NewGuid(); // QA / Manager who moves the card
        var assigneeId = Guid.NewGuid(); // Developer who did the work
        var skillId = Guid.NewGuid();

        context.Users.Add(new Sparnarok.Core.Entities.User { Id = moverId, Username = "QA_Manager", TotalXp = 0 });
        context.Users.Add(new Sparnarok.Core.Entities.User { Id = assigneeId, Username = "Dev_Hero", TotalXp = 50 });

        var quest = new Sparnarok.Core.Entities.Quest
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            Title = "Feature B2B",
            State = QuestState.InProgress,
            AssigneeId = assigneeId,
            Rewards = new List<Sparnarok.Core.Entities.QuestReward>
            {
                new Sparnarok.Core.Entities.QuestReward { SkillCategoryId = skillId, XpAmount = 100 }
            }
        };
        context.Quests.Add(quest);
        await context.SaveChangesAsync();

        // Act - QA Manager moves card to Completed
        var result = await service.UpdateQuestStatusAsync(quest.Id, QuestState.Completed, moverId, "QA_Manager");

        // Assert
        result.State.Should().Be(QuestState.Completed);

        // Mover should NOT gain XP
        var moverUser = await context.Users.FindAsync(moverId);
        moverUser!.TotalXp.Should().Be(0);

        // Assignee should receive the 100 XP
        var assigneeUser = await context.Users.FindAsync(assigneeId);
        assigneeUser!.TotalXp.Should().Be(150); // 50 + 100

        var progression = await context.UserSkillProgressions.FirstOrDefaultAsync(p => p.UserId == assigneeId);
        progression.Should().NotBeNull();
        progression!.CurrentXp.Should().Be(100);

        mockNotificationService.Verify(n => n.NotifyNewActivityAsync(
            partyId,
            Moq.It.Is<string>(msg => msg.Contains("Dev_Hero")),
            100,
            assigneeId,
            "Dev_Hero",
            quest.Id
        ), Moq.Times.Once);
    }

    [Fact]
    public async Task UpdateQuestStatusAsync_Rollback_WhenAssigned_ShouldDeductXpFromAssignee_NotCardMover()
    {
        // Arrange
        var partyId = Guid.NewGuid();
        using var context = GetDbContext(partyId);
        var mockNotificationService = new Moq.Mock<IRealTimeNotificationService>();
        var service = new QuestService(context, mockNotificationService.Object);

        var moverId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        context.Users.Add(new Sparnarok.Core.Entities.User { Id = moverId, Username = "QA_Manager", TotalXp = 10 });
        context.Users.Add(new Sparnarok.Core.Entities.User { Id = assigneeId, Username = "Dev_Hero", TotalXp = 150 });
        context.UserSkillProgressions.Add(new Sparnarok.Core.Entities.UserSkillProgression
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            UserId = assigneeId,
            SkillCategoryId = skillId,
            CurrentXp = 150,
            Level = 2
        });

        var quest = new Sparnarok.Core.Entities.Quest
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            Title = "Rejected Feature",
            State = QuestState.Completed,
            AssigneeId = assigneeId,
            Rewards = new List<Sparnarok.Core.Entities.QuestReward>
            {
                new Sparnarok.Core.Entities.QuestReward { SkillCategoryId = skillId, XpAmount = 100 }
            }
        };
        context.Quests.Add(quest);
        await context.SaveChangesAsync();

        // Act - Mover moves card back to InProgress
        var result = await service.UpdateQuestStatusAsync(quest.Id, QuestState.InProgress, moverId, "QA_Manager");

        // Assert
        result.State.Should().Be(QuestState.InProgress);

        var moverUser = await context.Users.FindAsync(moverId);
        moverUser!.TotalXp.Should().Be(10); // unchanged

        var assigneeUser = await context.Users.FindAsync(assigneeId);
        assigneeUser!.TotalXp.Should().Be(50); // 150 - 100

        var progression = await context.UserSkillProgressions.FirstOrDefaultAsync(p => p.UserId == assigneeId);
        progression!.CurrentXp.Should().Be(50);
    }

    [Fact]
    public async Task AssignQuestAsync_ShouldUpdateAssignee()
    {
        // Arrange
        var partyId = Guid.NewGuid();
        using var context = GetDbContext(partyId);
        var mockNotificationService = new Moq.Mock<IRealTimeNotificationService>();
        var service = new QuestService(context, mockNotificationService.Object);

        var newAssigneeId = Guid.NewGuid();
        context.Users.Add(new Sparnarok.Core.Entities.User { Id = newAssigneeId, Username = "Paladin", TotalXp = 0 });

        var quest = new Sparnarok.Core.Entities.Quest
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            Title = "Assign Me",
            State = QuestState.Pending
        };
        context.Quests.Add(quest);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AssignQuestAsync(quest.Id, newAssigneeId);

        // Assert
        result.AssigneeId.Should().Be(newAssigneeId);
        result.Assignee.Should().NotBeNull();
        result.Assignee!.Username.Should().Be("Paladin");

        var dbQuest = await context.Quests.FindAsync(quest.Id);
        dbQuest!.AssigneeId.Should().Be(newAssigneeId);
    }
}


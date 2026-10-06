using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Application.DTOs;
using Sparnarok.Application.Services;
using Sparnarok.Core.Enums;
using Sparnarok.Infrastructure.Data;
using Xunit;

namespace Sparnarok.Tests.Unit.Services;

public class QuestServiceTests
{
    private SparnarokDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<SparnarokDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SparnarokDbContext(options);
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
}

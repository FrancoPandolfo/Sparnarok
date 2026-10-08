using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sparnarok.Api.Hubs;
using Sparnarok.Api.Services;
using Sparnarok.Application.Interfaces;
using Sparnarok.Application.Services;
using Sparnarok.Core.Entities;
using Sparnarok.Core.Enums;
using Sparnarok.Infrastructure.Data;
using Xunit;

namespace Sparnarok.Tests.Unit.Services;

public class QuestServiceRealTimeTests
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
    public async Task CompleteQuest_ShouldSendSignalREventToTenantGroup()
    {
        // Arrange
        var partyId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        using var context = GetDbContext(partyId);
        
        context.Users.Add(new User { Id = userId, Username = "RealTimeHero", TotalXp = 0 });
        context.Parties.Add(new Party { Id = partyId });
        context.Quests.Add(new Quest 
        { 
            Id = questId, 
            PartyId = partyId, 
            State = QuestState.InProgress,
            Title = "Realtime Sync",
            Rewards = new List<QuestReward> { new QuestReward { SkillCategoryId = skillId, XpAmount = 100 } }
        });
        await context.SaveChangesAsync();

        var mockHubContext = new Mock<IHubContext<PartyHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();

        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group($"party-{partyId}")).Returns(mockClientProxy.Object);

        var notificationService = new SignalRNotificationService(mockHubContext.Object);
        var questService = new QuestService(context, notificationService);

        // Act
        await questService.UpdateQuestStatusAsync(questId, QuestState.Completed, userId);

        // Assert
        // Verify QuestUpdated was sent
        mockClientProxy.Verify(c => c.SendCoreAsync(
            "QuestUpdated", 
            It.Is<object[]>(args => args.Length == 1), 
            It.IsAny<CancellationToken>()
        ), Times.Once);

        // Verify NewPartyActivity was sent
        mockClientProxy.Verify(c => c.SendCoreAsync(
            "NewPartyActivity", 
            It.Is<object[]>(args => args.Length == 1), 
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}

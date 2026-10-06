using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Application.Interfaces;
using Sparnarok.Core.Entities;
using Sparnarok.Infrastructure.Data;
using Xunit;

namespace Sparnarok.Tests.Unit.Security;

public class PartySecurityTests
{
    private class FakeTenantService : ITenantService
    {
        private Guid _currentId;
        public Guid GetCurrentPartyId() => _currentId;
        public void SetCurrentPartyId(Guid partyId) => _currentId = partyId;
    }

    [Fact]
    public async Task GlobalQueryFilter_PreventsCrossTenantDataLeak()
    {
        // Arrange
        var tenantService = new FakeTenantService();
        var options = new DbContextOptionsBuilder<SparnarokDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var partyAId = Guid.NewGuid();
        var partyBId = Guid.NewGuid();

        using (var setupContext = new SparnarokDbContext(options, tenantService))
        {
            tenantService.SetCurrentPartyId(partyAId);
            setupContext.Quests.Add(new Quest { Id = Guid.NewGuid(), PartyId = partyAId, Title = "Quest A" });
            
            tenantService.SetCurrentPartyId(partyBId);
            setupContext.Quests.Add(new Quest { Id = Guid.NewGuid(), PartyId = partyBId, Title = "Quest B" });
            
            await setupContext.SaveChangesAsync();
        }

        // Act - Simulate request from User in Party A
        using (var actContext = new SparnarokDbContext(options, tenantService))
        {
            tenantService.SetCurrentPartyId(partyAId);
            
            var visibleQuests = await actContext.Quests.ToListAsync();

            // Assert
            Assert.Single(visibleQuests);
            Assert.Equal("Quest A", visibleQuests.First().Title);
            Assert.DoesNotContain(visibleQuests, q => q.Title == "Quest B");
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Api.Controllers;
using Sparnarok.Core.Entities;
using Sparnarok.Infrastructure.Data;
using Sparnarok.Application.Interfaces;
using Xunit;

namespace Sparnarok.Tests.Unit.Security;

public class AnalyticsSecurityTests
{
    private class FakeTenantService : ITenantService
    {
        private Guid _currentId;
        public Guid GetCurrentPartyId() => _currentId;
        public void SetCurrentPartyId(Guid partyId) => _currentId = partyId;
    }

    [Fact]
    public async Task GetAnalytics_RegularUser_Returns403Forbidden()
    {
        // Arrange
        var tenantService = new FakeTenantService();
        var options = new DbContextOptionsBuilder<SparnarokDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var partyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        tenantService.SetCurrentPartyId(partyId);

        using (var context = new SparnarokDbContext(options, tenantService))
        {
            context.PartyMembers.Add(new PartyMember 
            { 
                Id = Guid.NewGuid(), PartyId = partyId, UserId = userId, Role = "Member" 
            });
            await context.SaveChangesAsync();
        }

        using (var context = new SparnarokDbContext(options, tenantService))
        {
            var controller = new PartiesController(context);
            
            // Act
            var result = await controller.GetAnalytics(partyId, userId);
            
            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
        }
    }
}

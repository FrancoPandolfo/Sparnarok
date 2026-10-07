using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Api.Controllers;
using Sparnarok.Core.Entities;
using Sparnarok.Infrastructure.Data;
using Sparnarok.Application.Interfaces;
using Xunit;

namespace Sparnarok.Tests.Unit.Security;

public class PaywallSecurityTests
{
    private class FakeTenantService : ITenantService
    {
        private Guid _currentId;
        public Guid GetCurrentPartyId() => _currentId;
        public void SetCurrentPartyId(Guid partyId) => _currentId = partyId;
    }

    [Fact]
    public async Task UpdateMultipliers_WhenPartyIsNotPremium_Returns403ProRequired()
    {
        // Arrange
        var tenantService = new FakeTenantService();
        var options = new DbContextOptionsBuilder<SparnarokDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var partyId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        tenantService.SetCurrentPartyId(partyId);

        using (var context = new SparnarokDbContext(options, tenantService))
        {
            context.Parties.Add(new Party { Id = partyId, IsPremium = false });
            context.PartyMembers.Add(new PartyMember { Id = Guid.NewGuid(), PartyId = partyId, UserId = managerId, Role = "Manager" });
            await context.SaveChangesAsync();
        }

        using (var context = new SparnarokDbContext(options, tenantService))
        {
            var controller = new PartiesController(context);
            var multipliers = new Dictionary<string, decimal> { { "Urgente", 2.0m } };

            // Act
            var result = await controller.UpdateMultipliers(partyId, multipliers, managerId);
            
            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            Assert.Contains("ERR_REQUIRES_PRO", objectResult.Value!.ToString());
        }
    }
}

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

public class SessionZeroTests
{
    private class FakeTenantService : ITenantService
    {
        private Guid _currentId;
        public Guid GetCurrentPartyId() => _currentId;
        public void SetCurrentPartyId(Guid partyId) => _currentId = partyId;
    }

    [Fact]
    public async Task SessionZero_AlreadyCompleted_ReturnsBadRequest()
    {
        // Arrange
        var tenantService = new FakeTenantService();
        var options = new DbContextOptionsBuilder<SparnarokDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var partyId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        tenantService.SetCurrentPartyId(partyId);

        using (var context = new SparnarokDbContext(options, tenantService))
        {
            context.PartyMembers.Add(new PartyMember { Id = Guid.NewGuid(), PartyId = partyId, UserId = managerId, Role = "Manager" });
            context.PartyMembers.Add(new PartyMember { Id = Guid.NewGuid(), PartyId = partyId, UserId = targetUserId, Role = "Member", IsSessionZeroCompleted = true });
            await context.SaveChangesAsync();
        }

        using (var context = new SparnarokDbContext(options, tenantService))
        {
            var controller = new PartiesController(context);
            var payload = new Dictionary<string, int>();

            // Act
            var result = await controller.SessionZero(partyId, targetUserId, payload, managerId);
            
            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("ERR_SESSION_ZERO_ALREADY_COMPLETED", badRequestResult.Value?.ToString() ?? "");
        }
    }
}

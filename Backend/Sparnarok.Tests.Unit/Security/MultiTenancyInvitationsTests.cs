using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sparnarok.Api.Controllers;
using Sparnarok.Application.Interfaces;
using Sparnarok.Core.Entities;
using Sparnarok.Infrastructure.Data;
using Xunit;

namespace Sparnarok.Tests.Unit.Security;

public class MultiTenancyInvitationsTests
{
    private class FakeTenantService : ITenantService
    {
        private Guid _currentId;
        public Guid GetCurrentPartyId() => _currentId;
        public void SetCurrentPartyId(Guid partyId) => _currentId = partyId;
    }

    private SparnarokDbContext CreateContext(string dbName)
    {
        var tenantService = new FakeTenantService();
        var options = new DbContextOptionsBuilder<SparnarokDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new SparnarokDbContext(options, tenantService);
    }

    [Fact]
    public async Task InviteToParty_CreatesPendingInvitation_DoesNotDirectlyAddMember()
    {
        var dbName = Guid.NewGuid().ToString();
        var partyId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();

        using (var context = CreateContext(dbName))
        {
            context.Parties.Add(new Party { Id = partyId, Name = "Valhalla Devs", IsPremium = true });
            context.Users.Add(new User { Id = inviterId, Username = "Odin", Email = "odin@valhalla.com" });
            context.Users.Add(new User { Id = Guid.NewGuid(), Username = "Thor", Email = "thor@valhalla.com" });
            await context.SaveChangesAsync();
        }

        using (var context = CreateContext(dbName))
        {
            var controller = new PartiesController(context);
            var result = await controller.InviteToParty(partyId, new InviteDto { Email = "thor@valhalla.com" });

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Invitación enviada", okResult.Value!.ToString()!);

            // Verify invitation created
            var invitation = await context.PartyInvitations.FirstOrDefaultAsync(i => i.PartyId == partyId);
            Assert.NotNull(invitation);
            Assert.Equal("Pending", invitation.Status);
            Assert.Equal("thor@valhalla.com", invitation.InviteeEmail);

            // Verify user was NOT directly added to PartyMembers
            var members = await context.PartyMembers.Where(pm => pm.PartyId == partyId).ToListAsync();
            Assert.Empty(members);
        }
    }

    [Fact]
    public async Task AcceptInvitation_ChangesStatusToAccepted_AndInsertsPartyMember()
    {
        var dbName = Guid.NewGuid().ToString();
        var partyId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();

        using (var context = CreateContext(dbName))
        {
            context.Parties.Add(new Party { Id = partyId, Name = "Frontend Guild", IsPremium = false });
            context.Users.Add(new User { Id = inviteeId, Username = "Loki", Email = "loki@asgard.com" });
            context.PartyInvitations.Add(new PartyInvitation
            {
                Id = invitationId,
                PartyId = partyId,
                InviterId = Guid.NewGuid(),
                InviteeEmail = "loki@asgard.com",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        using (var context = CreateContext(dbName))
        {
            var controller = new MeController(context);
            var result = await controller.AcceptInvitation(invitationId, userId: inviteeId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Invitación aceptada exitosamente", okResult.Value!.ToString()!);

            // Verify status changed
            var invitation = await context.PartyInvitations.FirstOrDefaultAsync(i => i.Id == invitationId);
            Assert.NotNull(invitation);
            Assert.Equal("Accepted", invitation.Status);

            // Verify PartyMember inserted
            var membership = await context.PartyMembers.FirstOrDefaultAsync(pm => pm.PartyId == partyId && pm.UserId == inviteeId);
            Assert.NotNull(membership);
            Assert.Equal("Member", membership.Role);
        }
    }

    [Fact]
    public async Task GetMyParties_ReturnsAllUserWorkspaces()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var party1 = Guid.NewGuid();
        var party2 = Guid.NewGuid();

        using (var context = CreateContext(dbName))
        {
            context.Parties.Add(new Party { Id = party1, Name = "Guild Alpha" });
            context.Parties.Add(new Party { Id = party2, Name = "Guild Beta" });
            context.PartyMembers.Add(new PartyMember { Id = Guid.NewGuid(), PartyId = party1, UserId = userId, Role = "Manager" });
            context.PartyMembers.Add(new PartyMember { Id = Guid.NewGuid(), PartyId = party2, UserId = userId, Role = "Member" });
            await context.SaveChangesAsync();
        }

        using (var context = CreateContext(dbName))
        {
            var controller = new MeController(context);
            var result = await controller.GetMyParties(userId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(okResult.Value);
            var count = 0;
            foreach (var item in list) count++;
            Assert.Equal(2, count);
        }
    }

    [Fact]
    public async Task DevelopmentEnvironment_BypassesMonetizationLimit()
    {
        var dbName = Guid.NewGuid().ToString();
        var partyId = Guid.NewGuid();

        using (var context = CreateContext(dbName))
        {
            var party = new Party { Id = partyId, Name = "Free Guild", IsPremium = false };
            context.Parties.Add(party);
            // Add 3 members (limit reached for free)
            context.PartyMembers.Add(new PartyMember { Id = Guid.NewGuid(), PartyId = partyId, UserId = Guid.NewGuid() });
            context.PartyMembers.Add(new PartyMember { Id = Guid.NewGuid(), PartyId = partyId, UserId = Guid.NewGuid() });
            context.PartyMembers.Add(new PartyMember { Id = Guid.NewGuid(), PartyId = partyId, UserId = Guid.NewGuid() });
            context.Users.Add(new User { Id = Guid.NewGuid(), Username = "NewDev", Email = "newdev@sparnarok.com" });
            await context.SaveChangesAsync();
        }

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

        using (var context = CreateContext(dbName))
        {
            var controller = new PartiesController(context, mockEnv.Object);
            var result = await controller.InviteToParty(partyId, new InviteDto { Email = "newdev@sparnarok.com" });

            // Should succeed because dev bypasses limit
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Invitación enviada", okResult.Value!.ToString()!);
        }
    }
}

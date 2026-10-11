using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Api.Controllers;
using Sparnarok.Application.Interfaces;
using Sparnarok.Application.Services;
using Sparnarok.Core.Entities;
using Sparnarok.Infrastructure.Data;
using Xunit;

namespace Sparnarok.Tests.Unit.Services;

public class AutoDistributeAndColumnsTests
{
    private class FakeTenantService : ITenantService
    {
        private Guid _currentId = Guid.Empty;
        public Guid GetCurrentPartyId() => _currentId;
        public void SetCurrentPartyId(Guid partyId) => _currentId = partyId;
    }

    private SparnarokDbContext CreateContext(FakeTenantService tenantService)
    {
        var options = new DbContextOptionsBuilder<SparnarokDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SparnarokDbContext(options, tenantService);
    }

    [Fact]
    public async Task AutoDistributeXpAsync_AllocatesXpAcrossSkillTreeNodes()
    {
        // Arrange
        var tenant = new FakeTenantService();
        using var context = CreateContext(tenant);

        var userId = Guid.NewGuid();
        var partyId = Guid.NewGuid();
        tenant.SetCurrentPartyId(partyId);

        var user = new User
        {
            Id = userId,
            Username = "SeniorHero",
            Email = "senior@hero.com",
            TotalXp = 500
        };
        context.Users.Add(user);
        context.Parties.Add(new Party { Id = partyId, Name = "Alpha Guild" });
        context.PartyMembers.Add(new PartyMember { Id = Guid.NewGuid(), PartyId = partyId, UserId = userId, Role = "Member" });
        await context.SaveChangesAsync();

        var service = new ProfileService(context);

        // Act
        var result = await service.AutoDistributeXpAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.XpDistributed > 0);
        Assert.NotEmpty(result.UnlockedNodes);
        Assert.NotNull(result.UpdatedProfile);
    }

    [Fact]
    public async Task BoardColumnsController_GetColumns_SeedsDefaultsWhenEmpty()
    {
        // Arrange
        var tenant = new FakeTenantService();
        using var context = CreateContext(tenant);

        var partyId = Guid.NewGuid();
        tenant.SetCurrentPartyId(partyId);
        context.Parties.Add(new Party { Id = partyId, Name = "Guild" });
        await context.SaveChangesAsync();

        var controller = new BoardColumnsController(context, tenant);

        // Act
        var actionResult = await controller.GetColumns(partyId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var columns = Assert.IsAssignableFrom<IEnumerable<BoardColumn>>(okResult.Value);
        Assert.Equal(3, columns.Count());
    }

    [Fact]
    public async Task BoardColumnsController_CreateAndRenameAndDelete_WorksProperly()
    {
        // Arrange
        var tenant = new FakeTenantService();
        using var context = CreateContext(tenant);

        var partyId = Guid.NewGuid();
        tenant.SetCurrentPartyId(partyId);
        var controller = new BoardColumnsController(context, tenant);

        // Act 1: Create
        var createResult = await controller.CreateColumn(partyId, new ColumnCreateDto { Name = "Testing" });
        var createdOk = Assert.IsType<CreatedAtActionResult>(createResult);
        var newCol = Assert.IsType<BoardColumn>(createdOk.Value);
        Assert.Equal("Testing", newCol.Name);

        // Act 2: Update
        var updateResult = await controller.UpdateColumn(newCol.Id, new ColumnUpdateDto { Name = "QA Review" });
        var updatedOk = Assert.IsType<OkObjectResult>(updateResult);
        var updatedCol = Assert.IsType<BoardColumn>(updatedOk.Value);
        Assert.Equal("QA Review", updatedCol.Name);

        // Act 3: Delete
        var deleteResult = await controller.DeleteColumn(newCol.Id);
        Assert.IsType<OkObjectResult>(deleteResult);
    }
}

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Application.Interfaces;
using Sparnarok.Application.Services;
using Sparnarok.Core.Entities;
using Sparnarok.Infrastructure.Data;
using Xunit;

namespace Sparnarok.Tests.Unit.Services;

public class ProfileServiceTests
{
    private class FakeTenantService : ITenantService
    {
        public System.Guid GetCurrentPartyId() => System.Guid.Empty;
        public void SetCurrentPartyId(System.Guid partyId) {}
    }

    private DbContextOptions<SparnarokDbContext> CreateNewContextOptions()
    {
        return new DbContextOptionsBuilder<SparnarokDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetUserProfileAsync_CalculatesRankCorrectly()
    {
        // Arrange
        var options = CreateNewContextOptions();
        using var context = new SparnarokDbContext(options, new FakeTenantService());

        var userId = System.Guid.NewGuid();
        context.Users.Add(new User { Id = userId, Username = "Test", Email = "test@test.com", TotalXp = 350 });
        await context.SaveChangesAsync();

        var service = new ProfileService(context);

        // Act
        var result = await service.GetUserProfileAsync(userId);

        // Assert
        Assert.Equal("Guerrero", result.Rank);
        Assert.Equal(350, result.TotalXp);
    }
}

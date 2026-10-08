using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Api.Controllers;
using Sparnarok.Application.Interfaces;
using Sparnarok.Core.Entities;
using Sparnarok.Infrastructure.Data;
using Moq;
using Xunit;

namespace Sparnarok.Tests.Unit.Security;

public class WebhookSecurityTests
{
    private SparnarokDbContext GetDbContext(Guid partyId)
    {
        var options = new DbContextOptionsBuilder<SparnarokDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var mockTenant = new Mock<ITenantService>();
        mockTenant.Setup(t => t.GetCurrentPartyId()).Returns(partyId);
        return new SparnarokDbContext(options, mockTenant.Object);
    }

    private string GenerateSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    [Fact]
    public async Task ReceiveGitWebhook_WithInvalidHmac_ReturnsUnauthorized()
    {
        // Arrange
        var partyId = Guid.NewGuid();
        using var context = GetDbContext(partyId);
        context.Parties.Add(new Party { Id = partyId, WebhookSecret = "my-secret-key" });
        await context.SaveChangesAsync();

        var mockQuestService = new Mock<IQuestService>();
        var controller = new WebhooksController(context, mockQuestService.Object);

        var payload = "{\"action\": \"closed\"}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["X-Hub-Signature-256"] = "sha256=invalid-signature";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.ReceiveGitWebhook(partyId);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("ERR_UNAUTHORIZED", unauthorized.Value!.ToString());
    }

    [Fact]
    public async Task ReceiveGitWebhook_WithValidHmac_CompletesQuest()
    {
        // Arrange
        var partyId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        using var context = GetDbContext(partyId);
        context.Parties.Add(new Party { Id = partyId, WebhookSecret = "my-secret-key" });
        context.Quests.Add(new Quest { Id = questId, PartyId = partyId, DisplayId = "SPAR-12345678", AssigneeId = assigneeId, State = Sparnarok.Core.Enums.QuestState.InProgress });
        await context.SaveChangesAsync();

        var mockQuestService = new Mock<IQuestService>();
        var controller = new WebhooksController(context, mockQuestService.Object);

        var payload = JsonSerializer.Serialize(new {
            action = "closed",
            pull_request = new {
                merged = true,
                title = "Fixing auth [SPAR-12345678]"
            }
        });
        
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var signature = GenerateSignature(payload, "my-secret-key");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["X-Hub-Signature-256"] = signature;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.ReceiveGitWebhook(partyId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("true", okResult.Value!.ToString()!.ToLowerInvariant());
        
        mockQuestService.Verify(q => q.UpdateQuestStatusAsync(questId, Sparnarok.Core.Enums.QuestState.Completed, assigneeId), Times.Once);
        
        var eventLog = await context.PartyWebhookEvents.FirstOrDefaultAsync(e => e.PartyId == partyId);
        Assert.NotNull(eventLog);
        Assert.True(eventLog.IsSuccess);
    }
}

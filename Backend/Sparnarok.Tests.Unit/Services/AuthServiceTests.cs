using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Sparnarok.Application.DTOs;
using Sparnarok.Application.Services;
using Sparnarok.Core.Entities;
using Sparnarok.Infrastructure.Data;
using Xunit;

namespace Sparnarok.Tests.Unit.Services;

public class AuthServiceTests
{
    private SparnarokDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<SparnarokDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var mockTenant = new Mock<ITenantService>();
        mockTenant.Setup(t => t.GetCurrentPartyId()).Returns(Guid.Empty);
        
        return new SparnarokDbContext(options, mockTenant.Object);
    }

    [Fact]
    public async Task RegisterAsync_Fails_IfEmailAlreadyExists()
    {
        using var context = GetDbContext();
        var mockConfig = new Mock<IConfiguration>();
        var service = new AuthService(context, mockConfig.Object);

        context.Users.Add(new User { Id = Guid.NewGuid(), Email = "test@sparnarok.com", Username = "TestUser" });
        await context.SaveChangesAsync();

        var dto = new RegisterDto { Email = "test@sparnarok.com", Username = "NewUser", Password = "Password123" };

        var exception = await Assert.ThrowsAsync<ApplicationException>(() => service.RegisterAsync(dto));
        Assert.Equal("El email ya está registrado.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_Fails_IfCredentialsAreIncorrect()
    {
        using var context = GetDbContext();
        var mockConfig = new Mock<IConfiguration>();
        var service = new AuthService(context, mockConfig.Object);

        context.Users.Add(new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "hero@sparnarok.com", 
            Username = "Hero", 
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123") 
        });
        await context.SaveChangesAsync();

        var dto = new LoginDto { Email = "hero@sparnarok.com", Password = "WrongPassword123" };

        var exception = await Assert.ThrowsAsync<ApplicationException>(() => service.LoginAsync(dto));
        Assert.Equal("Credenciales incorrectas.", exception.Message);
    }
}

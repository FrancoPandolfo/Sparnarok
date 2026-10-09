using Microsoft.EntityFrameworkCore;
using Sparnarok.Application.Interfaces;
using Sparnarok.Application.Services;
using Sparnarok.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Volvemos a Postgres real ya que tenemos la password
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=sparnarok;Username=postgres;Password=password";

builder.Services.AddDbContext<SparnarokDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ISparnarokDbContext>(provider => provider.GetRequiredService<SparnarokDbContext>());
builder.Services.AddScoped<IQuestService, QuestService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IRealTimeNotificationService, Sparnarok.Api.Services.SignalRNotificationService>();
builder.Services.AddSignalR();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Sparnarok",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SparnarokApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "SPARNAROK_SUPER_SECRET_KEY_FOR_JWT_THAT_IS_LONG_ENOUGH_123456"))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/partyHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Sparnarok.Api.Hubs.PartyHub>("/api/partyHub");

app.Run();

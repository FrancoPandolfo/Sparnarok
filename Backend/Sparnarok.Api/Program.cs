using Microsoft.EntityFrameworkCore;
using Sparnarok.Application.Interfaces;
using Sparnarok.Application.Services;
using Sparnarok.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// En una app real, leeríamos de appsettings.json o variables de entorno.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=sparnarok;Username=postgres;Password=password";

builder.Services.AddDbContext<SparnarokDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ISparnarokDbContext>(provider => provider.GetRequiredService<SparnarokDbContext>());
builder.Services.AddScoped<IQuestService, QuestService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ITenantService, TenantService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

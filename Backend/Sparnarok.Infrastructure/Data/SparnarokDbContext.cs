using Microsoft.EntityFrameworkCore;
using Sparnarok.Core.Entities;
using Sparnarok.Application.Interfaces;
using System.Text.Json;
using System.Collections.Generic;

namespace Sparnarok.Infrastructure.Data;

public class SparnarokDbContext : DbContext, ISparnarokDbContext
{
    private readonly ITenantService _tenantService;

    public SparnarokDbContext(DbContextOptions<SparnarokDbContext> options, ITenantService tenantService) : base(options)
    {
        _tenantService = tenantService;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Quest> Quests { get; set; }
    public DbSet<SkillCategory> SkillCategories { get; set; }
    public DbSet<QuestReward> QuestRewards { get; set; }
    public DbSet<UserSkillProgression> UserSkillProgressions { get; set; }
    public DbSet<Party> Parties { get; set; }
    public DbSet<PartyMember> PartyMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Quest>().HasQueryFilter(e => e.PartyId == _tenantService.GetCurrentPartyId());
        modelBuilder.Entity<UserSkillProgression>().HasQueryFilter(e => e.PartyId == _tenantService.GetCurrentPartyId());

        modelBuilder.Entity<Party>()
            .Property(p => p.TagMultipliers)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, decimal>()
            );

        modelBuilder.Entity<Quest>()
            .Property(q => q.Tags)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>()
            );
    }
}

using Microsoft.EntityFrameworkCore;
using Sparnarok.Core.Entities;
using Sparnarok.Application.Interfaces;

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
    }
}

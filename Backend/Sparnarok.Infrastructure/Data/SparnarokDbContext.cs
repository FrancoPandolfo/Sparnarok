using Microsoft.EntityFrameworkCore;
using Sparnarok.Core.Entities;
using Sparnarok.Application.Interfaces;

namespace Sparnarok.Infrastructure.Data;

public class SparnarokDbContext : DbContext, ISparnarokDbContext
{
    public SparnarokDbContext(DbContextOptions<SparnarokDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Quest> Quests { get; set; }
    public DbSet<SkillCategory> SkillCategories { get; set; }
    public DbSet<QuestReward> QuestRewards { get; set; }
    public DbSet<UserSkillProgression> UserSkillProgressions { get; set; }
}

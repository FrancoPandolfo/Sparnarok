using Microsoft.EntityFrameworkCore;
using Sparnarok.Core.Entities;

namespace Sparnarok.Infrastructure.Data;

public class SparnarokDbContext : DbContext
{
    public SparnarokDbContext(DbContextOptions<SparnarokDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Quest> Quests { get; set; }
    public DbSet<SkillCategory> SkillCategories { get; set; }
    public DbSet<QuestReward> QuestRewards { get; set; }
}

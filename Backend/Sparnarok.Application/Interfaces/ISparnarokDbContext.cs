using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Core.Entities;

namespace Sparnarok.Application.Interfaces;

public interface ISparnarokDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<Quest> Quests { get; set; }
    DbSet<SkillCategory> SkillCategories { get; set; }
    DbSet<QuestReward> QuestRewards { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

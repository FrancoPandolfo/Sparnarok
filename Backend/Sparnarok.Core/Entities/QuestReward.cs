using System;

namespace Sparnarok.Core.Entities;

public class QuestReward
{
    public Guid Id { get; set; }
    
    public Guid QuestId { get; set; }
    public Quest Quest { get; set; } = null!;
    
    public Guid SkillCategoryId { get; set; }
    public SkillCategory SkillCategory { get; set; } = null!;
    
    public int XpAmount { get; set; } 
}

using System;

namespace Sparnarok.Core.Entities;

public class QuestReward
{
    public Guid Id { get; set; }
    
    public Guid QuestId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Quest Quest { get; set; } = null!;
    
    public Guid SkillCategoryId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public SkillCategory SkillCategory { get; set; } = null!;
    
    public int XpAmount { get; set; } 
}

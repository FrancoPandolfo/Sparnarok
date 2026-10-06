using System;
using System.Collections.Generic;
using Sparnarok.Core.Enums;

namespace Sparnarok.Core.Entities;

public class Quest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public QuestDifficulty Difficulty { get; set; }
    public QuestState State { get; set; } = QuestState.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }
    public ICollection<QuestReward> Rewards { get; set; } = new List<QuestReward>();
}

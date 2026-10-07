using System;
using System.Collections.Generic;
using Sparnarok.Core.Enums;

namespace Sparnarok.Application.DTOs;

public class CreateQuestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public QuestDifficulty Difficulty { get; set; }
    public List<string>? Tags { get; set; }
    public List<QuestRewardDto> Rewards { get; set; } = new();
}

public class QuestRewardDto
{
    public Guid SkillCategoryId { get; set; }
    public int XpAmount { get; set; }
}

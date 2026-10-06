using System;
using System.Collections.Generic;

namespace Sparnarok.Application.DTOs;

public class SkillProgressionDto
{
    public Guid SkillCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryDescription { get; set; } = string.Empty;
    public bool IsPremiumTier { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public int CurrentXp { get; set; }
    public int Level { get; set; }
}

public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int TotalXp { get; set; }
    public string Rank { get; set; } = string.Empty;
    public List<SkillProgressionDto> Skills { get; set; } = new();
}

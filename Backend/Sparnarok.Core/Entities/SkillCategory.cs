using System;

namespace Sparnarok.Core.Entities;

public class SkillCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPremiumTier { get; set; }
    public Guid? ParentCategoryId { get; set; }
}

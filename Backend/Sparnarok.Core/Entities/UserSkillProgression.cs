using System;
using Sparnarok.Core.Interfaces;

namespace Sparnarok.Core.Entities;

public class UserSkillProgression : IMustHaveParty
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public Guid UserId { get; set; }
    public Guid SkillCategoryId { get; set; }
    public int CurrentXp { get; set; }
    public int Level { get; set; }

    public User? User { get; set; }
    public SkillCategory? SkillCategory { get; set; }
}

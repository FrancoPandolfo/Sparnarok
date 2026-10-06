using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Application.DTOs;
using Sparnarok.Application.Interfaces;

namespace Sparnarok.Application.Services;

public class ProfileService : IProfileService
{
    private readonly ISparnarokDbContext _context;

    public ProfileService(ISparnarokDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new ApplicationException("User not found.");

        var progressions = await _context.UserSkillProgressions
            .Include(p => p.SkillCategory)
            .Where(p => p.UserId == userId)
            .ToListAsync();

        var allCategories = await _context.SkillCategories.ToListAsync();

        var skills = allCategories.Select(c => 
        {
            var prog = progressions.FirstOrDefault(p => p.SkillCategoryId == c.Id);
            return new SkillProgressionDto
            {
                SkillCategoryId = c.Id,
                CategoryName = c.Name,
                CategoryDescription = c.Description,
                IsPremiumTier = c.IsPremiumTier,
                ParentCategoryId = c.ParentCategoryId,
                CurrentXp = prog?.CurrentXp ?? 0,
                Level = prog?.Level ?? 0
            };
        }).ToList();

        var rank = user.TotalXp switch
        {
            < 100 => "Novato",
            < 300 => "Aventurero",
            < 1000 => "Guerrero",
            _ => "Leyenda"
        };

        return new UserProfileDto
        {
            UserId = user.Id,
            Username = user.Username,
            TotalXp = user.TotalXp,
            Rank = rank,
            Skills = skills
        };
    }
}

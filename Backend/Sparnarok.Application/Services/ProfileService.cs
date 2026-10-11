using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Application.DTOs;
using Sparnarok.Application.Interfaces;
using Sparnarok.Core.Entities;

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

    public async Task<AutoDistributeResultDto> AutoDistributeXpAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new ApplicationException("User not found.");

        var allCategories = await _context.SkillCategories.ToListAsync();
        if (!allCategories.Any())
        {
            var root = new SkillCategory { Id = Guid.NewGuid(), Name = "Iniciado", Description = "Fundamentos del Desarrollador", IsPremiumTier = false, ParentCategoryId = null };
            var frontend = new SkillCategory { Id = Guid.NewGuid(), Name = "Frontend", Description = "React, Tailwind, CSS Moderno", IsPremiumTier = false, ParentCategoryId = root.Id };
            var backend = new SkillCategory { Id = Guid.NewGuid(), Name = "Backend", Description = ".NET Core, SignalR, APIs", IsPremiumTier = false, ParentCategoryId = root.Id };
            var cloud = new SkillCategory { Id = Guid.NewGuid(), Name = "Arquitectura Cloud", Description = "Kubernetes, Microservicios, AWS/GCP", IsPremiumTier = true, ParentCategoryId = backend.Id };
            var devops = new SkillCategory { Id = Guid.NewGuid(), Name = "DevOps & CI/CD", Description = "Pipelines, Docker, Automatización", IsPremiumTier = false, ParentCategoryId = backend.Id };
            var ui = new SkillCategory { Id = Guid.NewGuid(), Name = "UI/UX Mágico", Description = "Diseño de Producto, Micro-interacciones", IsPremiumTier = false, ParentCategoryId = frontend.Id };

            allCategories = new List<SkillCategory> { root, frontend, backend, cloud, devops, ui };
            _context.SkillCategories.AddRange(allCategories);
            await _context.SaveChangesAsync();
        }

        var member = await _context.PartyMembers.IgnoreQueryFilters().FirstOrDefaultAsync(pm => pm.UserId == userId);
        var partyId = member?.PartyId ?? Guid.Empty;

        var progressions = await _context.UserSkillProgressions
            .Where(p => p.UserId == userId)
            .ToListAsync();

        int currentSpentXp = progressions.Sum(p => p.CurrentXp);
        int availableXp = user.TotalXp - currentSpentXp;

        if (availableXp <= 0)
        {
            availableXp = 300;
            user.TotalXp += 300;
        }

        int distributedSoFar = 0;
        var unlockedNodes = new List<UnlockedNodeDto>();
        const int costPerNode = 100;
        bool progressMade = true;

        while (availableXp >= costPerNode && progressMade)
        {
            progressMade = false;
            var unlockedCatIds = progressions.Where(p => p.Level > 0).Select(p => p.SkillCategoryId).ToHashSet();

            var candidates = allCategories
                .Where(c => c.ParentCategoryId == null || unlockedCatIds.Contains(c.ParentCategoryId.Value))
                .OrderBy(c => c.IsPremiumTier)
                .ThenBy(c => progressions.FirstOrDefault(p => p.SkillCategoryId == c.Id)?.Level ?? 0)
                .ToList();

            foreach (var candidate in candidates)
            {
                if (availableXp < costPerNode) break;

                var prog = progressions.FirstOrDefault(p => p.SkillCategoryId == candidate.Id);
                if (prog == null)
                {
                    prog = new UserSkillProgression
                    {
                        Id = Guid.NewGuid(),
                        PartyId = partyId,
                        UserId = userId,
                        SkillCategoryId = candidate.Id,
                        CurrentXp = costPerNode,
                        Level = 1
                    };
                    _context.UserSkillProgressions.Add(prog);
                    progressions.Add(prog);
                }
                else
                {
                    prog.CurrentXp += costPerNode;
                    prog.Level = 1 + (prog.CurrentXp / 100);
                }

                availableXp -= costPerNode;
                distributedSoFar += costPerNode;
                progressMade = true;

                var existingUnlocked = unlockedNodes.FirstOrDefault(u => u.SkillCategoryId == candidate.Id);
                if (existingUnlocked != null)
                {
                    existingUnlocked.NewLevel = prog.Level;
                    existingUnlocked.CurrentXp = prog.CurrentXp;
                }
                else
                {
                    unlockedNodes.Add(new UnlockedNodeDto
                    {
                        SkillCategoryId = candidate.Id,
                        CategoryName = candidate.Name,
                        NewLevel = prog.Level,
                        CurrentXp = prog.CurrentXp
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        var updatedProfile = await GetUserProfileAsync(userId);

        return new AutoDistributeResultDto
        {
            UserId = userId,
            XpDistributed = distributedSoFar,
            RemainingXp = availableXp,
            UnlockedNodes = unlockedNodes,
            UpdatedProfile = updatedProfile
        };
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Application.Interfaces;
using Sparnarok.Core.Entities;

namespace Sparnarok.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PartiesController : ControllerBase
{
    private readonly ISparnarokDbContext _context;

    public PartiesController(ISparnarokDbContext context)
    {
        _context = context;
    }

    [HttpPost("{partyId}/invite")]
    public async Task<IActionResult> InviteToParty(Guid partyId, [FromBody] InviteDto dto)
    {
        var party = await _context.Parties.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == partyId);
        
        if (party == null) return NotFound();

        if (!party.IsPremium && party.Members.Count >= 3)
        {
            return StatusCode(403, new { code = "ERR_PARTY_LIMIT_REACHED", message = "Límite de miembros alcanzado en plan gratuito." });
        }

        var newMember = new PartyMember
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            UserId = dto.UserId,
            JoinedAt = DateTime.UtcNow
        };

        _context.PartyMembers.Add(newMember);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Usuario invitado con éxito" });
    }

    [HttpGet("{partyId}/analytics")]
    public async Task<IActionResult> GetAnalytics(Guid partyId, [FromQuery] Guid userId)
    {
        var membership = await _context.PartyMembers
            .FirstOrDefaultAsync(pm => pm.PartyId == partyId && pm.UserId == userId);
            
        if (membership == null || membership.Role != "Manager")
        {
            return StatusCode(403, new { code = "ERR_UNAUTHORIZED", message = "Sólo los mánagers pueden ver las analíticas." });
        }

        var skillProgressions = await _context.UserSkillProgressions
            .Include(usp => usp.SkillCategory)
            .ToListAsync();

        var distribution = skillProgressions
            .GroupBy(usp => usp.SkillCategory?.Name ?? "Unknown")
            .Select(g => new {
                category = g.Key,
                totalXp = g.Sum(x => x.CurrentXp)
            })
            .ToList();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentQuests = await _context.Quests
            .Include(q => q.Assignee)
            .Include(q => q.Rewards)
            .Where(q => q.State == Sparnarok.Core.Enums.QuestState.Completed && q.CreatedAt >= thirtyDaysAgo && q.AssigneeId != null)
            .ToListAsync();

        var topPerformers = recentQuests
            .GroupBy(q => new { Id = q.AssigneeId, Name = q.Assignee?.Username ?? "Unknown" })
            .Select(g => new {
                userId = g.Key.Id,
                username = g.Key.Name,
                xpGained = g.Sum(q => q.Rewards.Sum(r => r.XpAmount))
            })
            .OrderByDescending(x => x.xpGained)
            .Take(3)
            .ToList();

        return Ok(new { distribution, topPerformers });
    }

    [HttpPost("{partyId}/members/{userId}/session-zero")]
    public async Task<IActionResult> SessionZero(Guid partyId, Guid userId, [FromBody] System.Collections.Generic.Dictionary<string, int> initialXp, [FromQuery] Guid requesterId)
    {
        var requester = await _context.PartyMembers
            .FirstOrDefaultAsync(pm => pm.PartyId == partyId && pm.UserId == requesterId);
            
        if (requester == null || requester.Role != "Manager")
        {
            return StatusCode(403, new { code = "ERR_UNAUTHORIZED", message = "Sólo los mánagers pueden ejecutar la Sesión Cero." });
        }

        var targetMember = await _context.PartyMembers
            .FirstOrDefaultAsync(pm => pm.PartyId == partyId && pm.UserId == userId);

        if (targetMember == null) return NotFound("Usuario no encontrado en la Party");

        if (targetMember.IsSessionZeroCompleted)
        {
            return BadRequest(new { code = "ERR_SESSION_ZERO_ALREADY_COMPLETED", message = "La calibración de la Sesión Cero ya fue realizada para este usuario." });
        }

        var categories = await _context.SkillCategories.ToListAsync();

        foreach (var (categoryName, xpAmount) in initialXp)
        {
            var category = categories.FirstOrDefault(c => c.Name == categoryName);
            if (category != null)
            {
                var progression = new UserSkillProgression
                {
                    Id = Guid.NewGuid(),
                    PartyId = partyId,
                    UserId = userId,
                    SkillCategoryId = category.Id,
                    CurrentXp = xpAmount,
                    Level = 1 + (xpAmount / 100)
                };
                _context.UserSkillProgressions.Add(progression);
            }
        }

        targetMember.IsSessionZeroCompleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Sesión Cero completada exitosamente." });
    }

    [HttpPut("{partyId}/settings/multipliers")]
    public async Task<IActionResult> UpdateMultipliers(Guid partyId, [FromBody] System.Collections.Generic.Dictionary<string, decimal> multipliers, [FromQuery] Guid requesterId)
    {
        var requester = await _context.PartyMembers
            .Include(pm => pm.Party)
            .FirstOrDefaultAsync(pm => pm.PartyId == partyId && pm.UserId == requesterId);

        if (requester == null || requester.Role != "Manager")
        {
            return StatusCode(403, new { code = "ERR_UNAUTHORIZED", message = "Sólo los mánagers pueden editar las reglas." });
        }

        if (requester.Party != null && !requester.Party.IsPremium)
        {
            return StatusCode(403, new { code = "ERR_REQUIRES_PRO", message = "Desbloquea las Reglas Custom con Sparnarok Pro." });
        }

        requester.Party!.TagMultipliers = multipliers;
        await _context.SaveChangesAsync();

        return Ok(requester.Party.TagMultipliers);
    }
}

public class InviteDto
{
    public Guid UserId { get; set; }
}

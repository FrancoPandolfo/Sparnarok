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

    [HttpGet("{partyId}")]
    public async Task<IActionResult> GetPartyDetails(Guid partyId)
    {
        var party = await _context.Parties.IgnoreQueryFilters().Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == partyId);
        if (party == null) return NotFound();
        return Ok(new { id = party.Id, name = party.Name, isPremium = party.IsPremium, memberCount = party.Members.Count });
    }

    [HttpPost("{partyId}/invite")]
    public async Task<IActionResult> InviteToParty(Guid partyId, [FromBody] InviteDto dto)
    {
        var party = await _context.Parties.IgnoreQueryFilters().Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == partyId);
        
        if (party == null) return NotFound(new { message = "Party no encontrada" });

        var targetUserId = dto.UserId;
        if (targetUserId == Guid.Empty)
        {
            var user = await _context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => (!string.IsNullOrEmpty(dto.Email) && u.Email == dto.Email) ||
                                          (!string.IsNullOrEmpty(dto.Username) && u.Username == dto.Username) ||
                                          (!string.IsNullOrEmpty(dto.Email) && u.Username == dto.Email));
            if (user == null)
            {
                return NotFound(new { message = "Héroe no encontrado por email o username." });
            }
            targetUserId = user.Id;
        }

        var existingMember = await _context.PartyMembers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(pm => pm.PartyId == partyId && pm.UserId == targetUserId);

        if (existingMember != null)
        {
            return Ok(new { message = "El usuario ya forma parte de esta Party", partyId = party.Id, partyName = party.Name });
        }

        if (!party.IsPremium && party.Members.Count >= 3)
        {
            return StatusCode(403, new { code = "ERR_PARTY_LIMIT_REACHED", message = "Límite de miembros alcanzado en plan gratuito." });
        }

        var previousMembership = await _context.PartyMembers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(pm => pm.UserId == targetUserId);

        if (previousMembership != null)
        {
            previousMembership.PartyId = partyId;
            previousMembership.Role = "Member";
        }
        else
        {
            var newMember = new PartyMember
            {
                Id = Guid.NewGuid(),
                PartyId = partyId,
                UserId = targetUserId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };
            _context.PartyMembers.Add(newMember);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Usuario invitado con éxito", partyId = party.Id, partyName = party.Name });
    }

    [HttpPost("{partyId}/join")]
    public async Task<IActionResult> JoinParty(Guid partyId, [FromQuery] Guid userId)
    {
        var party = await _context.Parties.IgnoreQueryFilters().Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == partyId);
        if (party == null) return NotFound(new { message = "Party no encontrada" });

        var membership = await _context.PartyMembers.IgnoreQueryFilters().FirstOrDefaultAsync(pm => pm.UserId == userId);
        if (membership != null)
        {
            membership.PartyId = partyId;
        }
        else
        {
            _context.PartyMembers.Add(new PartyMember
            {
                Id = Guid.NewGuid(),
                PartyId = partyId,
                UserId = userId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Te has unido a la Party", partyId = party.Id, partyName = party.Name });
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
    public Guid UserId { get; set; } = Guid.Empty;
    public string? Email { get; set; }
    public string? Username { get; set; }
}

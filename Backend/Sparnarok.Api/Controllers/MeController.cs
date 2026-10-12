using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Sparnarok.Application.Interfaces;
using Sparnarok.Core.Entities;

namespace Sparnarok.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeController : ControllerBase
{
    private readonly ISparnarokDbContext _context;
    private readonly IWebHostEnvironment? _env;

    public MeController(ISparnarokDbContext context, IWebHostEnvironment? env = null)
    {
        _context = context;
        _env = env;
    }

    private bool IsPartyPremium(Party? party)
    {
        if (_env != null && _env.IsDevelopment()) return true;
        return party?.IsPremium ?? false;
    }

    private Guid ResolveUserId(Guid? explicitUserId)
    {
        if (explicitUserId.HasValue && explicitUserId.Value != Guid.Empty)
        {
            return explicitUserId.Value;
        }

        var subClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User?.FindFirst("sub")?.Value;

        if (Guid.TryParse(subClaim, out var parsedSub))
        {
            return parsedSub;
        }

        return Guid.Empty;
    }

    [HttpGet("parties")]
    public async Task<IActionResult> GetMyParties([FromQuery] Guid? userId = null)
    {
        var currentUserId = ResolveUserId(userId);
        if (currentUserId == Guid.Empty)
        {
            return Unauthorized(new { message = "Usuario no autenticado." });
        }

        var memberships = await _context.PartyMembers.IgnoreQueryFilters()
            .Include(pm => pm.Party)
            .Where(pm => pm.UserId == currentUserId)
            .ToListAsync();

        if (memberships.Count == 0)
        {
            var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (user != null)
            {
                var party = new Party
                {
                    Id = Guid.NewGuid(),
                    Name = $"Party de {user.Username}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Parties.Add(party);

                var newMember = new PartyMember
                {
                    Id = Guid.NewGuid(),
                    PartyId = party.Id,
                    UserId = user.Id,
                    Role = "Manager",
                    JoinedAt = DateTime.UtcNow
                };
                _context.PartyMembers.Add(newMember);

                var defaultColumns = new[]
                {
                    new BoardColumn { Id = Guid.NewGuid(), PartyId = party.Id, Name = "Pendiente", Order = 0, CreatedAt = DateTime.UtcNow },
                    new BoardColumn { Id = Guid.NewGuid(), PartyId = party.Id, Name = "En Progreso", Order = 1, CreatedAt = DateTime.UtcNow },
                    new BoardColumn { Id = Guid.NewGuid(), PartyId = party.Id, Name = "Completada", Order = 2, CreatedAt = DateTime.UtcNow }
                };
                _context.BoardColumns.AddRange(defaultColumns);

                await _context.SaveChangesAsync();

                memberships = new List<PartyMember> { newMember };
                newMember.Party = party;
            }
        }

        var parties = memberships
            .Where(pm => pm.Party != null)
            .Select(pm => new
            {
                id = pm.Party!.Id,
                name = pm.Party.Name,
                role = pm.Role,
                isPremium = IsPartyPremium(pm.Party),
                joinedAt = pm.JoinedAt
            })
            .OrderBy(p => p.name)
            .ToList();

        return Ok(parties);
    }

    [HttpGet("invitations")]
    public async Task<IActionResult> GetMyInvitations([FromQuery] Guid? userId = null)
    {
        var currentUserId = ResolveUserId(userId);
        if (currentUserId == Guid.Empty)
        {
            return Unauthorized(new { message = "Usuario no autenticado." });
        }

        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == currentUserId);
        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        var invitations = await _context.PartyInvitations.IgnoreQueryFilters()
            .Include(i => i.Party)
            .Include(i => i.Inviter)
            .Where(i => i.InviteeEmail.ToLower() == user.Email.ToLower() && i.Status == "Pending")
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var result = invitations.Select(i => new
        {
            id = i.Id,
            partyId = i.PartyId,
            partyName = i.Party?.Name ?? "Party",
            inviterId = i.InviterId,
            inviterName = i.Inviter?.Username ?? "Aliado",
            inviteeEmail = i.InviteeEmail,
            status = i.Status,
            createdAt = i.CreatedAt
        });

        return Ok(result);
    }

    [HttpPost("invitations/{id}/accept")]
    public async Task<IActionResult> AcceptInvitation(Guid id, [FromQuery] Guid? userId = null)
    {
        var currentUserId = ResolveUserId(userId);
        if (currentUserId == Guid.Empty)
        {
            return Unauthorized(new { message = "Usuario no autenticado." });
        }

        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == currentUserId);
        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        var invitation = await _context.PartyInvitations.IgnoreQueryFilters()
            .Include(i => i.Party)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invitation == null)
        {
            return NotFound(new { message = "Invitación no encontrada." });
        }

        if (invitation.Status == "Accepted")
        {
            return Ok(new
            {
                message = "La invitación ya fue aceptada.",
                partyId = invitation.PartyId,
                partyName = invitation.Party?.Name ?? "Party"
            });
        }

        invitation.Status = "Accepted";

        var existingMembership = await _context.PartyMembers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(pm => pm.PartyId == invitation.PartyId && pm.UserId == currentUserId);

        if (existingMembership == null)
        {
            var newMember = new PartyMember
            {
                Id = Guid.NewGuid(),
                PartyId = invitation.PartyId,
                UserId = currentUserId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };
            _context.PartyMembers.Add(newMember);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Invitación aceptada exitosamente",
            partyId = invitation.PartyId,
            partyName = invitation.Party?.Name ?? "Party"
        });
    }

    [HttpPost("invitations/{id}/decline")]
    public async Task<IActionResult> DeclineInvitation(Guid id, [FromQuery] Guid? userId = null)
    {
        var currentUserId = ResolveUserId(userId);
        if (currentUserId == Guid.Empty)
        {
            return Unauthorized(new { message = "Usuario no autenticado." });
        }

        var invitation = await _context.PartyInvitations.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == id);
        if (invitation == null)
        {
            return NotFound(new { message = "Invitación no encontrada." });
        }

        invitation.Status = "Declined";
        await _context.SaveChangesAsync();

        return Ok(new { message = "Invitación rechazada." });
    }
}

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
}

public class InviteDto
{
    public Guid UserId { get; set; }
}

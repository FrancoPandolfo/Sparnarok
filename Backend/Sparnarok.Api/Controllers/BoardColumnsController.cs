using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Application.Interfaces;
using Sparnarok.Core.Entities;

namespace Sparnarok.Api.Controllers;

[ApiController]
public class BoardColumnsController : ControllerBase
{
    private readonly ISparnarokDbContext _context;
    private readonly ITenantService _tenantService;

    public BoardColumnsController(ISparnarokDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    [HttpGet("api/parties/{partyId}/columns")]
    [HttpGet("api/columns")]
    public async Task<IActionResult> GetColumns([FromRoute] Guid? partyId)
    {
        var targetPartyId = partyId ?? _tenantService.GetCurrentPartyId();
        if (targetPartyId == Guid.Empty)
        {
            var firstParty = await _context.Parties.FirstOrDefaultAsync();
            targetPartyId = firstParty?.Id ?? Guid.Empty;
        }

        var columns = await _context.BoardColumns
            .IgnoreQueryFilters()
            .Where(c => c.PartyId == targetPartyId)
            .OrderBy(c => c.Order)
            .ToListAsync();

        if (!columns.Any() && targetPartyId != Guid.Empty)
        {
            var defaultColumns = new List<BoardColumn>
            {
                new BoardColumn { Id = Guid.NewGuid(), PartyId = targetPartyId, Name = "Pendiente", Order = 0, CreatedAt = DateTime.UtcNow },
                new BoardColumn { Id = Guid.NewGuid(), PartyId = targetPartyId, Name = "En Progreso", Order = 1, CreatedAt = DateTime.UtcNow },
                new BoardColumn { Id = Guid.NewGuid(), PartyId = targetPartyId, Name = "Completada", Order = 2, CreatedAt = DateTime.UtcNow }
            };
            _context.BoardColumns.AddRange(defaultColumns);
            await _context.SaveChangesAsync();
            columns = defaultColumns;
        }

        return Ok(columns);
    }

    [HttpPost("api/parties/{partyId}/columns")]
    [HttpPost("api/columns")]
    public async Task<IActionResult> CreateColumn([FromRoute] Guid? partyId, [FromBody] ColumnCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "El nombre de la columna no puede estar vacío." });

        var targetPartyId = partyId ?? _tenantService.GetCurrentPartyId();
        if (targetPartyId == Guid.Empty)
        {
            var firstParty = await _context.Parties.FirstOrDefaultAsync();
            targetPartyId = firstParty?.Id ?? Guid.Empty;
        }

        var maxOrder = await _context.BoardColumns
            .IgnoreQueryFilters()
            .Where(c => c.PartyId == targetPartyId)
            .Select(c => (int?)c.Order)
            .MaxAsync() ?? -1;

        var newColumn = new BoardColumn
        {
            Id = Guid.NewGuid(),
            PartyId = targetPartyId,
            Name = dto.Name.Trim(),
            Order = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.BoardColumns.Add(newColumn);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetColumns), new { partyId = targetPartyId }, newColumn);
    }

    [HttpPut("api/parties/{partyId}/columns/{id}")]
    [HttpPut("api/columns/{id}")]
    public async Task<IActionResult> UpdateColumn([FromRoute] Guid id, [FromBody] ColumnUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "El nombre de la columna no puede estar vacío." });

        var column = await _context.BoardColumns
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (column == null)
            return NotFound(new { message = "Columna no encontrada." });

        column.Name = dto.Name.Trim();
        await _context.SaveChangesAsync();

        return Ok(column);
    }

    [HttpDelete("api/parties/{partyId}/columns/{id}")]
    [HttpDelete("api/columns/{id}")]
    public async Task<IActionResult> DeleteColumn([FromRoute] Guid id)
    {
        var column = await _context.BoardColumns
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (column == null)
            return NotFound(new { message = "Columna no encontrada." });

        _context.BoardColumns.Remove(column);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Columna eliminada con éxito." });
    }
}

public class ColumnCreateDto
{
    public string Name { get; set; } = string.Empty;
}

public class ColumnUpdateDto
{
    public string Name { get; set; } = string.Empty;
}

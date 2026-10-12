using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Sparnarok.Application.DTOs;
using Sparnarok.Application.Interfaces;
using Sparnarok.Core.Entities;

namespace Sparnarok.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestsController : ControllerBase
{
    private readonly IQuestService _questService;
    private readonly ITenantService _tenantService;

    public QuestsController(IQuestService questService, ITenantService tenantService)
    {
        _questService = questService;
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<IActionResult> GetQuests([FromQuery] Guid? partyId = null)
    {
        var targetPartyId = partyId.HasValue && partyId.Value != Guid.Empty
            ? partyId.Value
            : _tenantService.GetCurrentPartyId();

        if (targetPartyId == Guid.Empty)
        {
            return Ok(new List<Quest>());
        }

        var quests = await _questService.GetQuestsByPartyAsync(targetPartyId);
        return Ok(quests);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuest([FromBody] CreateQuestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var quest = await _questService.CreateQuestAsync(request);
            return CreatedAtAction(nameof(GetQuestById), new { id = quest.Id }, quest);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public IActionResult GetQuestById(Guid id)
    {
        return Ok();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateQuestStatusDto request, [FromQuery] Guid? userId = null)
    {
        try
        {
            var resolvedUserId = userId.HasValue && userId.Value != Guid.Empty ? userId.Value : Guid.Empty;
            if (resolvedUserId == Guid.Empty)
            {
                var subClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User?.FindFirst("sub")?.Value;
                if (Guid.TryParse(subClaim, out var parsedSub))
                {
                    resolvedUserId = parsedSub;
                }
            }

            var quest = await _questService.UpdateQuestStatusAsync(id, request.State, resolvedUserId);
            return Ok(quest);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

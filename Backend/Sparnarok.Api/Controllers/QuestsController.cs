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
        var result = quests.Select(q => new
        {
            id = q.Id.ToString(),
            displayId = q.DisplayId,
            title = q.Title,
            description = q.Description,
            difficulty = (int)q.Difficulty,
            state = (int)q.State,
            assigneeId = q.AssigneeId?.ToString(),
            assigneeName = q.Assignee?.Username,
            tags = q.Tags,
            rewards = q.Rewards.Select(r => new
            {
                id = r.Id.ToString(),
                skillCategoryId = r.SkillCategoryId.ToString(),
                xpAmount = r.XpAmount
            }).ToList(),
            createdAt = q.CreatedAt
        });
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuest([FromBody] CreateQuestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var quest = await _questService.CreateQuestAsync(request);
            var result = new
            {
                id = quest.Id.ToString(),
                displayId = quest.DisplayId,
                title = quest.Title,
                description = quest.Description,
                difficulty = (int)quest.Difficulty,
                state = (int)quest.State,
                assigneeId = quest.AssigneeId?.ToString(),
                assigneeName = quest.Assignee?.Username,
                tags = quest.Tags,
                rewards = quest.Rewards.Select(r => new
                {
                    id = r.Id.ToString(),
                    skillCategoryId = r.SkillCategoryId.ToString(),
                    xpAmount = r.XpAmount
                }).ToList(),
                createdAt = quest.CreatedAt
            };
            return CreatedAtAction(nameof(GetQuestById), new { id = quest.Id }, result);
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

    [HttpPatch("{id}/assign")]
    public async Task<IActionResult> AssignQuest(Guid id, [FromBody] AssignQuestDto request)
    {
        try
        {
            var quest = await _questService.AssignQuestAsync(id, request.AssigneeId);
            return Ok(new
            {
                id = quest.Id.ToString(),
                displayId = quest.DisplayId,
                title = quest.Title,
                description = quest.Description,
                difficulty = (int)quest.Difficulty,
                state = (int)quest.State,
                assigneeId = quest.AssigneeId?.ToString(),
                assigneeName = quest.Assignee?.Username,
                tags = quest.Tags,
                rewards = quest.Rewards.Select(r => new
                {
                    id = r.Id.ToString(),
                    skillCategoryId = r.SkillCategoryId.ToString(),
                    xpAmount = r.XpAmount
                }).ToList(),
                createdAt = quest.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateQuestStatusDto request, [FromQuery] Guid? userId = null)
    {
        try
        {
            var resolvedUserId = Guid.Empty;
            if (userId.HasValue && userId.Value != Guid.Empty && userId.Value != Guid.Parse("11111111-1111-1111-1111-111111111111"))
            {
                resolvedUserId = userId.Value;
            }
            else if (request.UserId.HasValue && request.UserId.Value != Guid.Empty && request.UserId.Value != Guid.Parse("11111111-1111-1111-1111-111111111111"))
            {
                resolvedUserId = request.UserId.Value;
            }

            if (resolvedUserId == Guid.Empty)
            {
                var subClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User?.FindFirst("sub")?.Value;
                if (Guid.TryParse(subClaim, out var parsedSub))
                {
                    resolvedUserId = parsedSub;
                }
            }

            var resolvedUsername = !string.IsNullOrWhiteSpace(request.Username)
                ? request.Username
                : (User?.FindFirst("username")?.Value ?? User?.FindFirst(ClaimTypes.Name)?.Value ?? User?.Identity?.Name);

            var quest = await _questService.UpdateQuestStatusAsync(id, request.State, resolvedUserId, resolvedUsername);
            return Ok(new
            {
                id = quest.Id.ToString(),
                displayId = quest.DisplayId,
                title = quest.Title,
                description = quest.Description,
                difficulty = (int)quest.Difficulty,
                state = (int)quest.State,
                assigneeId = quest.AssigneeId?.ToString(),
                assigneeName = quest.Assignee?.Username,
                tags = quest.Tags,
                rewards = quest.Rewards.Select(r => new
                {
                    id = r.Id.ToString(),
                    skillCategoryId = r.SkillCategoryId.ToString(),
                    xpAmount = r.XpAmount
                }).ToList(),
                createdAt = quest.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

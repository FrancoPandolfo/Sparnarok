using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Sparnarok.Application.DTOs;
using Sparnarok.Application.Interfaces;

namespace Sparnarok.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestsController : ControllerBase
{
    private readonly IQuestService _questService;

    public QuestsController(IQuestService questService)
    {
        _questService = questService;
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
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateQuestStatusDto request, [FromQuery] Guid userId)
    {
        try
        {
            var quest = await _questService.UpdateQuestStatusAsync(id, request.State, userId);
            return Ok(quest);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

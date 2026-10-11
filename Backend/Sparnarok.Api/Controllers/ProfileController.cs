using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Sparnarok.Application.Interfaces;

namespace Sparnarok.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("skills")]
    public async Task<IActionResult> GetProfile([FromQuery] Guid userId)
    {
        try
        {
            var profile = await _profileService.GetUserProfileAsync(userId);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("skills/auto-distribute")]
    public async Task<IActionResult> AutoDistributeSkills([FromQuery] Guid? userId)
    {
        try
        {
            var targetUserId = userId ?? Guid.Empty;
            if (targetUserId == Guid.Empty)
            {
                var subClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? User?.FindFirst("sub")?.Value;
                if (Guid.TryParse(subClaim, out var parsedSub))
                {
                    targetUserId = parsedSub;
                }
            }

            if (targetUserId == Guid.Empty)
            {
                return BadRequest(new { message = "Se requiere el identificador de usuario (userId)." });
            }

            var result = await _profileService.AutoDistributeXpAsync(targetUserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

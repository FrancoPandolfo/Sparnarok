using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sparnarok.Application.Interfaces;
using Sparnarok.Core.Entities;
using Sparnarok.Core.Enums;
using Sparnarok.Infrastructure.Data;

namespace Sparnarok.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly ISparnarokDbContext _context;
    private readonly IQuestService _questService;

    public WebhooksController(ISparnarokDbContext context, IQuestService questService)
    {
        _context = context;
        _questService = questService;
    }

    [HttpPost("git/{partyId}")]
    public async Task<IActionResult> ReceiveGitWebhook(Guid partyId)
    {
        var party = await _context.Parties.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == partyId);
        if (party == null) return NotFound("Party not found");

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        
        // Security check
        if (!string.IsNullOrEmpty(party.WebhookSecret))
        {
            var signatureHeader = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            if (string.IsNullOrEmpty(signatureHeader) || !IsValidSignature(payload, party.WebhookSecret, signatureHeader))
            {
                return Unauthorized(new { code = "ERR_UNAUTHORIZED", message = "Firma HMAC inválida." });
            }
        }

        bool isSuccess = false;
        string errorMessage = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            
            // Basic GitHub PR merged parsing
            if (root.TryGetProperty("action", out var actionElement) && actionElement.GetString() == "closed" &&
                root.TryGetProperty("pull_request", out var prElement))
            {
                if (prElement.TryGetProperty("merged", out var mergedElement) && mergedElement.GetBoolean())
                {
                    var title = prElement.GetProperty("title").GetString() ?? string.Empty;
                    var match = Regex.Match(title, @"\[(?<id>SPAR-[A-Z0-9]{8})\]");
                    if (match.Success)
                    {
                        var displayId = match.Groups["id"].Value;
                        var quest = await _context.Quests.IgnoreQueryFilters().FirstOrDefaultAsync(q => q.PartyId == partyId && q.DisplayId == displayId);
                        
                        if (quest != null && quest.State != QuestState.Completed)
                        {
                            // Temporarily set tenant context for the service
                            // If we don't have it set by middleware, the service might fail.
                            // We assume Assignee gets the XP.
                            if (quest.AssigneeId.HasValue)
                            {
                                await _questService.UpdateQuestStatusAsync(quest.Id, QuestState.Completed, quest.AssigneeId.Value);
                                isSuccess = true;
                            }
                            else
                            {
                                errorMessage = "La Quest no tiene Asignado. No se otorgó XP.";
                            }
                        }
                        else
                        {
                            errorMessage = quest == null ? "Quest no encontrada." : "La Quest ya estaba completada.";
                        }
                    }
                    else
                    {
                        errorMessage = "No se encontró el patrón de Quest ID en el título del PR.";
                    }
                }
                else
                {
                    errorMessage = "PR cerrado pero no fusionado.";
                }
            }
            else
            {
                errorMessage = "Evento ignorado.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }

        // Record history
        _context.PartyWebhookEvents.Add(new PartyWebhookEvent
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            EventType = "Git PR",
            PayloadPreview = payload.Length > 200 ? payload.Substring(0, 200) + "..." : payload,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage
        });
        await _context.SaveChangesAsync();

        return Ok(new { success = isSuccess, message = errorMessage });
    }

    [HttpPut("settings/{partyId}/secret")]
    public async Task<IActionResult> RegenerateSecret(Guid partyId, [FromQuery] Guid requesterId)
    {
        var requester = await _context.PartyMembers.FirstOrDefaultAsync(pm => pm.PartyId == partyId && pm.UserId == requesterId);
        if (requester == null || requester.Role != "Manager") return Forbid();

        var party = await _context.Parties.FirstOrDefaultAsync(p => p.Id == partyId);
        if (party == null) return NotFound();

        party.WebhookSecret = GenerateSecureSecret();
        await _context.SaveChangesAsync();

        return Ok(new { secret = party.WebhookSecret });
    }

    [HttpGet("settings/{partyId}/events")]
    public async Task<IActionResult> GetWebhookEvents(Guid partyId)
    {
        var events = await _context.PartyWebhookEvents
            .Where(e => e.PartyId == partyId)
            .OrderByDescending(e => e.ReceivedAt)
            .Take(10)
            .ToListAsync();
        return Ok(events);
    }

    private bool IsValidSignature(string payload, string secret, string signatureHeader)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var hashString = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        return hashString == signatureHeader.ToLowerInvariant();
    }

    private string GenerateSecureSecret()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

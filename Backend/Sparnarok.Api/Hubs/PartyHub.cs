using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Sparnarok.Application.Interfaces;

namespace Sparnarok.Api.Hubs;

[Authorize]
public class PartyHub : Hub
{
    private readonly ITenantService _tenantService;

    public PartyHub(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var partyIdStr = httpContext?.Request.Query["partyId"].ToString();
        if (string.IsNullOrWhiteSpace(partyIdStr))
        {
            partyIdStr = httpContext?.Request.Query["party_id"].ToString();
        }
        if (string.IsNullOrWhiteSpace(partyIdStr))
        {
            partyIdStr = Context.User?.FindFirst("partyId")?.Value;
        }

        if (Guid.TryParse(partyIdStr, out var partyId) && partyId != Guid.Empty)
        {
            _tenantService.SetCurrentPartyId(partyId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"party-{partyId.ToString().ToLower()}");
        }
        else
        {
            var fallbackPartyId = _tenantService.GetCurrentPartyId();
            if (fallbackPartyId != Guid.Empty)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"party-{fallbackPartyId.ToString().ToLower()}");
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var httpContext = Context.GetHttpContext();
        var partyIdStr = httpContext?.Request.Query["partyId"].ToString();
        if (string.IsNullOrWhiteSpace(partyIdStr))
        {
            partyIdStr = httpContext?.Request.Query["party_id"].ToString();
        }
        if (string.IsNullOrWhiteSpace(partyIdStr))
        {
            partyIdStr = Context.User?.FindFirst("partyId")?.Value;
        }

        if (Guid.TryParse(partyIdStr, out var partyId) && partyId != Guid.Empty)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"party-{partyId.ToString().ToLower()}");
        }
        else
        {
            var fallbackPartyId = _tenantService.GetCurrentPartyId();
            if (fallbackPartyId != Guid.Empty)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"party-{fallbackPartyId.ToString().ToLower()}");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinPartyGroup(string partyId)
    {
        if (Guid.TryParse(partyId, out var parsedId) && parsedId != Guid.Empty)
        {
            _tenantService.SetCurrentPartyId(parsedId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"party-{parsedId.ToString().ToLower()}");
        }
    }
}

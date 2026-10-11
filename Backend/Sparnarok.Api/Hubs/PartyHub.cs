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
        var partyIdClaim = Context.User?.FindFirst("partyId")?.Value;
        if (Guid.TryParse(partyIdClaim, out var partyId) && partyId != Guid.Empty)
        {
            _tenantService.SetCurrentPartyId(partyId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"party-{partyId}");
        }
        else
        {
            var fallbackPartyId = _tenantService.GetCurrentPartyId();
            if (fallbackPartyId != Guid.Empty)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"party-{fallbackPartyId}");
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var partyIdClaim = Context.User?.FindFirst("partyId")?.Value;
        if (Guid.TryParse(partyIdClaim, out var partyId) && partyId != Guid.Empty)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"party-{partyId}");
        }
        else
        {
            var fallbackPartyId = _tenantService.GetCurrentPartyId();
            if (fallbackPartyId != Guid.Empty)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"party-{fallbackPartyId}");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}

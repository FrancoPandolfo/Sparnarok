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
        var partyId = _tenantService.GetCurrentPartyId();
        if (partyId != Guid.Empty)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"party-{partyId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var partyId = _tenantService.GetCurrentPartyId();
        if (partyId != Guid.Empty)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"party-{partyId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}

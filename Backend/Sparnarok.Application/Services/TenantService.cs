using System;
using Sparnarok.Application.Interfaces;

namespace Sparnarok.Application.Services;

public class TenantService : ITenantService
{
    private Guid _currentPartyId;

    public Guid GetCurrentPartyId() => _currentPartyId;
    
    public void SetCurrentPartyId(Guid partyId)
    {
        _currentPartyId = partyId;
    }
}

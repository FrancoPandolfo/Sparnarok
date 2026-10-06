using System;

namespace Sparnarok.Application.Interfaces;

public interface ITenantService
{
    Guid GetCurrentPartyId();
    void SetCurrentPartyId(Guid partyId);
}

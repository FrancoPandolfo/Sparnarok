using System;
using System.Threading.Tasks;
using Sparnarok.Application.DTOs;

namespace Sparnarok.Application.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto> GetUserProfileAsync(Guid userId);
}

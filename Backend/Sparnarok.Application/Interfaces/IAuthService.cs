using System;
using System.Threading.Tasks;
using Sparnarok.Application.DTOs;

namespace Sparnarok.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterDto dto);
    Task<AuthResultDto> LoginAsync(LoginDto dto);
}

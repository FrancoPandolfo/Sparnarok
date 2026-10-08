using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Sparnarok.Application.DTOs;
using Sparnarok.Application.Interfaces;
using Sparnarok.Core.Entities;
using BCrypt.Net;

namespace Sparnarok.Application.Services;

public class AuthService : IAuthService
{
    private readonly ISparnarokDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ISparnarokDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
        {
            throw new ApplicationException("El email ya está registrado.");
        }

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };
        _context.Users.Add(user);

        // Auto Onboarding
        var partyId = Guid.NewGuid();
        var party = new Party
        {
            Id = partyId,
            Name = $"Party de {dto.Username}",
            IsPremium = false
        };
        _context.Parties.Add(party);

        var member = new PartyMember
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            UserId = userId,
            Role = "Manager"
        };
        _context.PartyMembers.Add(member);

        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user, partyId);
        return new AuthResultDto { Token = token, Username = user.Username };
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new ApplicationException("Credenciales incorrectas.");
        }

        var member = await _context.PartyMembers.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.UserId == user.Id);
        var partyId = member?.PartyId ?? Guid.Empty;

        var token = GenerateJwtToken(user, partyId);
        return new AuthResultDto { Token = token, Username = user.Username };
    }

    private string GenerateJwtToken(User user, Guid partyId)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var keyString = jwtSettings["Key"] ?? "SPARNAROK_SUPER_SECRET_KEY_FOR_JWT_THAT_IS_LONG_ENOUGH_123456";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("partyId", partyId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "Sparnarok",
            audience: jwtSettings["Audience"] ?? "SparnarokApp",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

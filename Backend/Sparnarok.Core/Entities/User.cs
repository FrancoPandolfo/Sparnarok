using System;
using System.Collections.Generic;

namespace Sparnarok.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int GlobalLevel { get; set; } = 1;
    public int TotalXp { get; set; } = 0;
    
    public ICollection<Quest> AssignedQuests { get; set; } = new List<Quest>();
}

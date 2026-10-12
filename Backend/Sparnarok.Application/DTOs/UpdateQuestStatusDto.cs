using Sparnarok.Core.Enums;

namespace Sparnarok.Application.DTOs;

public class UpdateQuestStatusDto
{
    public QuestState State { get; set; }
    public string? Username { get; set; }
    public System.Guid? UserId { get; set; }
}


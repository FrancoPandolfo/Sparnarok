using System.Threading.Tasks;
using Sparnarok.Core.Entities;
using Sparnarok.Application.DTOs;

namespace Sparnarok.Application.Interfaces;

public interface IQuestService
{
    Task<Quest> CreateQuestAsync(CreateQuestDto dto);
}

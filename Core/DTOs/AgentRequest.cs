using System.Collections.Generic;
using Core.Entities;

namespace Core.DTOs;

public class AgentRequest
{
    public List<AgentMessage> History { get; set; } = new();
    public string? CartId { get; set; }
}

using System.Collections.Generic;

namespace Core.Entities;

public class AgentMessage
{
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class AgentResponse
{
    public string Text { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = new();
    public ActionConfirmation? Confirmation { get; set; }
}

public class ActionConfirmation
{
    public string Action { get; set; } = string.Empty;
    public string ToolCallId { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

using System.Threading.Tasks;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController(IAIShoppingAgentService agentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GetAgentResponse([FromBody] AgentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message cannot be empty.");
        }

        // CartId can be null if the user doesn't have an active cart, but the agent handles null or empty.
        var response = await agentService.GetAgentChatResponseAsync(request.Message, request.CartId ?? string.Empty);
        
        return Ok(new { response });
    }
}

public class AgentRequest
{
    public string Message { get; set; } = string.Empty;
    public string? CartId { get; set; }
}

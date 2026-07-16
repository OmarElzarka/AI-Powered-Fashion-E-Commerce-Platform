using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class AgentController(IAIShoppingAgentService agentService) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<AgentResponse>> Chat([FromBody] AgentRequest request)
    {
        try
        {
            var cartId = request.CartId;
            if (string.IsNullOrWhiteSpace(cartId))
            {
                cartId = Guid.NewGuid().ToString();
            }

            var response = await agentService.GetAgentChatResponseAsync(request.History, cartId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in agent chat: {ex.Message}");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmAction([FromBody] ActionConfirmation request, [FromQuery] string? cartId)
    {
        var cart = await agentService.ConfirmActionAsync(request, cartId);

        if (cart != null)
        {
            return Ok(new { message = $"Successfully processed {request.Action}.", cartId = cart.Id });
        }

        return BadRequest("Action could not be processed.");
    }
}

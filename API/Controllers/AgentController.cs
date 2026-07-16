using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController(IAIShoppingAgentService agentService, ICartService cartService, IProductRepository productRepository) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GetAgentResponse([FromBody] AgentRequest request)
    {
        if (request.History == null || request.History.Count == 0)
        {
            return BadRequest("History cannot be empty.");
        }

        var response = await agentService.GetAgentChatResponseAsync(request.History, request.CartId ?? string.Empty);
        
        return Ok(response);
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmAction([FromBody] ActionConfirmation request, [FromQuery] string? cartId)
    {
        if (request.Action == "AddToCart")
        {
            var productId = ((JsonElement)request.Parameters["productId"]).GetInt32();
            var quantity = ((JsonElement)request.Parameters["quantity"]).GetInt32();
            
            var product = await productRepository.GetProductByIdAsync(productId);
            if (product == null) return NotFound("Product not found");

            if (string.IsNullOrWhiteSpace(cartId)) cartId = Guid.NewGuid().ToString();

            var cart = await cartService.GetCartAsync(cartId) ?? new ShoppingCart { Id = cartId };
            var existingItem = cart.Items.Find(i => i.ProductId == productId);
            if (existingItem != null) existingItem.Quantity += quantity;
            else cart.Items.Add(new CartItem { ProductId = product.Id, ProductName = product.Name, Price = product.Price, PictureUrl = product.ImageUrl, Brand = product.Brand, Type = product.Category, Quantity = quantity });
            
            await cartService.SetCartAsync(cart);
            return Ok(new { message = $"Successfully added {quantity} of '{product.Name}' to the cart.", cartId = cart.Id });
        }
        else if (request.Action == "RemoveFromCart")
        {
            if (string.IsNullOrWhiteSpace(cartId)) return BadRequest("CartId is required to remove items.");

            var productId = ((JsonElement)request.Parameters["productId"]).GetInt32();
            var quantity = ((JsonElement)request.Parameters["quantity"]).GetInt32();

            var cart = await cartService.GetCartAsync(cartId);
            if (cart == null) return NotFound("Cart not found");

            var item = cart.Items.Find(i => i.ProductId == productId);
            if (item != null)
            {
                if (item.Quantity <= quantity) cart.Items.Remove(item);
                else item.Quantity -= quantity;
                await cartService.SetCartAsync(cart);
            }
            return Ok(new { message = $"Successfully removed product from the cart." });
        }

        return BadRequest("Unknown action.");
    }
}

public class AgentRequest
{
    public List<AgentMessage> History { get; set; } = new();
    public string? CartId { get; set; }
}

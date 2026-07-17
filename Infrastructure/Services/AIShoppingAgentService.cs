using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Infrastructure.Services;

public class AIShoppingAgentService(
    ICartService cartService,
    IProductRepository productRepository,
    IChatCompletionService chatCompletionService,
    Kernel kernel,
    Infrastructure.Plugins.ShoppingAgentPlugin shoppingAgentPlugin,
    Infrastructure.Services.AgentResponseContext agentContext,
    ILogger<AIShoppingAgentService> logger) : IAIShoppingAgentService
{
    private readonly ChatHistory _chatHistory = new();

    public async Task<AgentResponse> GetAgentChatResponseAsync(List<AgentMessage> history, string cartId)
    {
        logger.LogInformation("Processing AI shopping request for cart {CartId}", cartId);
        
        if (!kernel.Plugins.Contains("ShoppingAgentPlugin"))
        {
            kernel.Plugins.AddFromObject(shoppingAgentPlugin, "ShoppingAgentPlugin");
        }

        _chatHistory.AddSystemMessage(
            "You are the STYLE AI Fashion Assistant. You have two main recommendation modes depending on the user's intent:\n\n" +
            "1. OUTFIT REQUESTS (e.g., 'winter outfit', 'wedding attire'): You MUST assemble a cohesive, complete outfit. " +
            "An outfit must include 1 Top (shirt/t-shirt), 1 Bottom (pants/jeans), 1 Pair of Shoes, and optionally 1 Outerwear/Accessory. " +
            "Call the SearchCatalog tool to find each matching piece. In your response, explicitly explain WHY these pieces work well together in terms of style, color, season, and occasion.\n\n" +
            "2. PRODUCT SEARCHES (e.g., 'black t-shirts', 'shoes', 'dresses'): You MUST NOT build an outfit. Instead, find 5-10 items of that specific requested category using SearchCatalog and present them so the user can browse.\n\n" +
            "IMPORTANT INSTRUCTIONS:\n" +
            "- Always format your responses cleanly using proper headings, bold text, and bullet points.\n" +
            "- To recommend products, you MUST call the `RecommendProducts` tool with the product IDs so they appear visually to the user. DO NOT just write their names without calling the tool.\n" +
            "- When modifying the cart, use the provided AddToCart/RemoveFromCart tools. The tools will pause execution and ask the user for confirmation.\n" +
            $"The user's cart ID is: {cartId}. Use this for cart-related tools."
        );

        foreach (var msg in history)
        {
            if (msg.Role == "user")
                _chatHistory.AddUserMessage(msg.Text);
            else if (msg.Role == "assistant")
                _chatHistory.AddAssistantMessage(msg.Text);
            else if (msg.Role == "system")
                _chatHistory.AddSystemMessage(msg.Text);
        }

        var executionSettings = new Microsoft.SemanticKernel.Connectors.Google.GeminiPromptExecutionSettings 
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var sw = Stopwatch.StartNew();
        var response = await chatCompletionService.GetChatMessageContentAsync(
            _chatHistory, 
            executionSettings: executionSettings,
            kernel: kernel
        );
        sw.Stop();
        
        logger.LogInformation("AI request completed in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

        return new AgentResponse
        {
            Text = response.Content ?? "I couldn't process your request.",
            Products = agentContext.ProductsToDisplay,
            Confirmation = agentContext.PendingConfirmation
        };
    }

    public Task<IReadOnlyList<Product>> SearchByNaturalLanguageAsync(string query, int count = 10)
    {
        return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    }

    public Task<string> GetProductComparisonAsync(int productId1, int productId2)
    {
        return Task.FromResult("AI comparison feature coming soon.");
    }

    public Task<string> GetProductExplanationAsync(int productId)
    {
        return Task.FromResult("AI product explanation feature coming soon.");
    }

    public async Task<ShoppingCart?> ConfirmActionAsync(ActionConfirmation request, string? cartId)
    {
        if (request.Action == "AddToCart")
        {
            var productId = ((System.Text.Json.JsonElement)request.Parameters["productId"]).GetInt32();
            var quantity = ((System.Text.Json.JsonElement)request.Parameters["quantity"]).GetInt32();
            
            var product = await productRepository.GetProductByIdAsync(productId);
            if (product == null) return null;

            if (string.IsNullOrWhiteSpace(cartId)) cartId = Guid.NewGuid().ToString();

            var cart = await cartService.GetCartAsync(cartId) ?? new ShoppingCart { Id = cartId };
            var existingItem = cart.Items.Find(i => i.ProductId == productId);
            if (existingItem != null) existingItem.Quantity += quantity;
            else cart.Items.Add(new CartItem { ProductId = product.Id, ProductName = product.Name, Price = product.Price, PictureUrl = product.ImageUrl, Brand = product.Brand, Type = product.Category, Quantity = quantity });
            
            return await cartService.SetCartAsync(cart);
        }
        else if (request.Action == "RemoveFromCart")
        {
            if (string.IsNullOrWhiteSpace(cartId)) return null;

            var productId = ((System.Text.Json.JsonElement)request.Parameters["productId"]).GetInt32();
            var quantity = ((System.Text.Json.JsonElement)request.Parameters["quantity"]).GetInt32();

            var cart = await cartService.GetCartAsync(cartId);
            if (cart == null) return null;

            var item = cart.Items.Find(i => i.ProductId == productId);
            if (item != null)
            {
                if (item.Quantity <= quantity) cart.Items.Remove(item);
                else item.Quantity -= quantity;
                return await cartService.SetCartAsync(cart);
            }
            return cart;
        }

        return null;
    }
}

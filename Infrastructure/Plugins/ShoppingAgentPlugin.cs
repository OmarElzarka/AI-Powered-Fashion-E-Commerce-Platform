using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Core.Entities;
using Core.Interfaces;
using Microsoft.SemanticKernel;
using Infrastructure.Services;

namespace Infrastructure.Plugins;

public class ShoppingAgentPlugin(
    IProductRepository productRepository,
    ICartService cartService,
    ITextEmbeddingService textEmbeddingService,
    IRecommendationService recommendationService,
    AgentResponseContext agentContext)
{
    [KernelFunction("SearchCatalog")]
    [System.ComponentModel.Description("Searches the product catalog for items matching a description using AI vector search. Use this to find clothes, outfits, or answer style queries. Returns a list of products in JSON format. IMPORTANT: Once you receive the products, you MUST call the RecommendProducts tool to actually show them to the user.")]
    public async Task<string> SearchCatalogAsync(
        [System.ComponentModel.Description("The search query describing the product or outfit (e.g. 'casual summer outfit under 100')")] string query)
    {
        var queryVector = await textEmbeddingService.GenerateEmbeddingAsync(query);
        var products = await recommendationService.SearchByVectorAsync(queryVector, limit: 10);

        if (!products.Any())
        {
            return $"No exact matches found for '{query}'. Tell the user we don't have exactly what they are looking for right now.";
        }

        var results = products.Select(p => new { p.Id, p.Name, p.Price, p.Category, p.Brand }).ToList();
        return JsonSerializer.Serialize(results);
    }

    [KernelFunction("RecommendProducts")]
    [System.ComponentModel.Description("Displays specific products to the user visually in the chat UI. Call this whenever you recommend products.")]
    public async Task<string> RecommendProductsAsync(
        [System.ComponentModel.Description("List of product IDs to display")] int[] productIds)
    {
        var toDisplay = await productRepository.GetProductsByIdsAsync(productIds);
        
        foreach (var p in toDisplay)
        {
            if (!agentContext.ProductsToDisplay.Any(existing => existing.Id == p.Id))
            {
                agentContext.ProductsToDisplay.Add(p);
            }
        }
        return $"Successfully sent {toDisplay.Count} products to the user's UI for display.";
    }

    [KernelFunction("GetCartContents")]
    [Description("Gets the current contents of the user's shopping cart.")]
    public async Task<string> GetCartContentsAsync(
        [Description("The unique identifier of the user's cart")] string cartId)
    {
        if (string.IsNullOrWhiteSpace(cartId)) return "No active cart ID provided.";

        var cart = await cartService.GetCartAsync(cartId);
        if (cart == null || cart.Items.Count == 0)
        {
            return "The cart is currently empty.";
        }

        var items = cart.Items.Select(i => new { i.ProductId, i.ProductName, i.Quantity, i.Price }).ToList();
        return JsonSerializer.Serialize(items);
    }

    [KernelFunction("AddToCart")]
    [System.ComponentModel.Description("Adds a specific product to the user's shopping cart. This action requires user confirmation.")]
    public async Task<string> AddToCartAsync(
        [System.ComponentModel.Description("The ID of the product to add")] int productId,
        [System.ComponentModel.Description("The quantity to add (usually 1)")] int quantity,
        [System.ComponentModel.Description("The unique identifier of the user's cart")] string cartId)
    {
        var product = await productRepository.GetProductByIdAsync(productId);
        if (product == null) return $"Failed to add to cart: Product with ID {productId} not found.";

        agentContext.PendingConfirmation = new ActionConfirmation
        {
            Action = "AddToCart",
            ToolCallId = "pending",
            Parameters = new Dictionary<string, object>
            {
                { "productId", productId },
                { "quantity", quantity }
            }
        };

        return $"Action paused. Tell the user you are waiting for them to click 'Yes' to add {quantity} of '{product.Name}' to their cart.";
    }

    [KernelFunction("RemoveFromCart")]
    [System.ComponentModel.Description("Removes a specific product from the user's shopping cart or reduces its quantity. This action requires user confirmation.")]
    public async Task<string> RemoveFromCartAsync(
        [System.ComponentModel.Description("The ID of the product to remove")] int productId,
        [System.ComponentModel.Description("The quantity to remove")] int quantity,
        [System.ComponentModel.Description("The unique identifier of the user's cart")] string cartId)
    {
        agentContext.PendingConfirmation = new ActionConfirmation
        {
            Action = "RemoveFromCart",
            ToolCallId = "pending",
            Parameters = new Dictionary<string, object>
            {
                { "productId", productId },
                { "quantity", quantity }
            }
        };

        return $"Action paused. Tell the user you are waiting for them to click 'Yes' to remove product ID {productId} from their cart.";
    }
}

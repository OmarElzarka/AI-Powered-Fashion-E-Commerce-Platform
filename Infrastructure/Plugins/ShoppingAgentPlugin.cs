using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Microsoft.SemanticKernel;

namespace Infrastructure.Plugins;

public class ShoppingAgentPlugin(
    IProductRepository productRepository,
    ICartService cartService)
{
    [KernelFunction("SearchCatalog")]
    [Description("Searches the product catalog for items matching a description. Use this to find clothes or outfits.")]
    public async Task<string> SearchCatalogAsync(
        [Description("The search query describing the product or outfit (e.g. 'blue denim jacket' or 'summer dress')")] string query)
    {
        // For semantic search, we would use vector search, but since we don't have the textEmbeddingService directly here,
        // we can fetch all products and do a simple in-memory search, or if we had access to recommendationService.SearchByVectorAsync,
        // we could use it if we inject ITextEmbeddingService.
        // Actually, we can just return a list of products by getting all and doing a basic string match for simplicity in this plugin,
        // or we can just fetch some products. 
        // Let's use GetProductsAsync from the repository. We can pass the query as a brand or type if it matches, but since it's a general query,
        // we'll get a list of products and filter them manually for this prototype.
        
        var products = await productRepository.GetProductsAsync(null, null, null);
        
        var filtered = products
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                        p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        p.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .Select(p => new { p.Id, p.Name, p.Price, p.Category, p.Brand })
            .ToList();

        if (!filtered.Any())
        {
            // If simple text match fails, fallback to returning top 5 general products as "suggestions"
            filtered = products.Take(5).Select(p => new { p.Id, p.Name, p.Price, p.Category, p.Brand }).ToList();
            return $"No exact matches found for '{query}', but here are some popular items: " + JsonSerializer.Serialize(filtered);
        }

        return JsonSerializer.Serialize(filtered);
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
    [Description("Adds a specific product to the user's shopping cart.")]
    public async Task<string> AddToCartAsync(
        [Description("The ID of the product to add")] int productId,
        [Description("The quantity to add (usually 1)")] int quantity,
        [Description("The unique identifier of the user's cart")] string cartId)
    {
        if (string.IsNullOrWhiteSpace(cartId)) return "Failed to add to cart: No cart ID provided.";
        
        var product = await productRepository.GetProductByIdAsync(productId);
        if (product == null) return $"Failed to add to cart: Product with ID {productId} not found.";

        var cart = await cartService.GetCartAsync(cartId) ?? new ShoppingCart { Id = cartId };

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                PictureUrl = product.ImageUrl,
                Brand = product.Brand,
                Type = product.Category,
                Quantity = quantity
            });
        }

        await cartService.SetCartAsync(cart);
        return $"Successfully added {quantity} of '{product.Name}' to the cart.";
    }

    [KernelFunction("RemoveFromCart")]
    [Description("Removes a specific product from the user's shopping cart or reduces its quantity.")]
    public async Task<string> RemoveFromCartAsync(
        [Description("The ID of the product to remove")] int productId,
        [Description("The quantity to remove")] int quantity,
        [Description("The unique identifier of the user's cart")] string cartId)
    {
        if (string.IsNullOrWhiteSpace(cartId)) return "Failed to remove from cart: No cart ID provided.";

        var cart = await cartService.GetCartAsync(cartId);
        if (cart == null || cart.Items.Count == 0) return "Failed: Cart is already empty.";

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return $"Failed: Product with ID {productId} is not in the cart.";

        if (item.Quantity <= quantity)
        {
            cart.Items.Remove(item);
        }
        else
        {
            item.Quantity -= quantity;
        }

        await cartService.SetCartAsync(cart);
        return $"Successfully removed {(item.Quantity <= quantity ? "all" : quantity.ToString())} of product ID {productId} from the cart.";
    }
}

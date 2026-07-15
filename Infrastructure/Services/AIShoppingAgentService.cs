using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Infrastructure.Services;

public class AIShoppingAgentService(
    IChatCompletionService chatCompletionService,
    Kernel kernel,
    Infrastructure.Plugins.ShoppingAgentPlugin shoppingAgentPlugin) : IAIShoppingAgentService
{
    private readonly ChatHistory _chatHistory = new();

    public async Task<string> GetAgentChatResponseAsync(string userMessage, string cartId)
    {
        if (!kernel.Plugins.Contains("ShoppingAgentPlugin"))
        {
            kernel.Plugins.AddFromObject(shoppingAgentPlugin, "ShoppingAgentPlugin");
        }

        if (_chatHistory.Count == 0)
        {
            _chatHistory.AddSystemMessage(
                "You are an autonomous AI Shopping Agent for our fashion e-commerce platform. " +
                "Your goal is to help users find products, build outfits, and manage their shopping carts. " +
                "You have access to tools to search the catalog, check the cart contents, and add/remove items. " +
                $"The user's cart ID is: {cartId}. Always use this cart ID when calling cart-related tools. " +
                "Feel free to reason about outfits and use your tools to complete the user's request. " +
                "Do not hallucinate products. Only use products returned by your tools."
            );
        }

        _chatHistory.AddUserMessage(userMessage);

        var executionSettings = new Microsoft.SemanticKernel.Connectors.Google.GeminiPromptExecutionSettings 
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var response = await chatCompletionService.GetChatMessageContentAsync(
            _chatHistory, 
            executionSettings: executionSettings,
            kernel: kernel
        );

        _chatHistory.AddAssistantMessage(response.Content ?? "");

        return response.Content ?? "I couldn't process your request.";
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
}

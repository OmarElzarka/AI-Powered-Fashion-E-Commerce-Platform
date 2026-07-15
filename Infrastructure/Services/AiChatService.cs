using System.Text.Json;
using System.Threading.Tasks;
using Core.Interfaces;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Infrastructure.Services;

public class AiChatService(
    ITextEmbeddingService textEmbeddingService,
    IRecommendationService recommendationService,
    IChatCompletionService chatCompletionService) : IAiChatService
{
    public async Task<string> GetChatResponseAsync(string userMessage)
    {
        // 1. Semantic Retrieval (RAG)
        // Embed the user's query
        var queryVector = await textEmbeddingService.GenerateEmbeddingAsync(userMessage);
        
        // Find top 5 products matching this query
        var relevantProducts = await recommendationService.SearchByVectorAsync(queryVector, limit: 5);

        // Serialize the retrieved products to provide as context
        var options = new JsonSerializerOptions { WriteIndented = true };
        var productsJson = JsonSerializer.Serialize(relevantProducts, options);

        // 2. Build the Prompt
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(
            "You are a helpful and expert virtual fashion assistant for our e-commerce platform. " +
            "Your goal is to help the user find the best outfits or products based on their requests. " +
            "Below is a list of the top matching products from our catalog that correspond to the user's query. " +
            "You MUST only recommend products from this provided list. Do not invent or hallucinate any products that are not on this list. " +
            "When mentioning a product, refer to its Name and Price. " +
            "If none of the provided products seem to match what the user is asking for, politely apologize and say we don't have exactly what they are looking for right now.\n\n" +
            $"CATALOG CONTEXT:\n{productsJson}"
        );
        chatHistory.AddUserMessage(userMessage);

        // 3. Call Gemini via Semantic Kernel
        var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory);
        return response.Content ?? "I'm sorry, I couldn't process your request.";
    }
}

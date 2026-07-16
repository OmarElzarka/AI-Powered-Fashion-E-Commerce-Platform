using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.Interfaces;

/// <summary>
/// AI Shopping Agent service interface. Future-ready for conversational commerce,
/// product comparison, natural language search, and autonomous cart management.
/// </summary>
public interface IAIShoppingAgentService
{
    Task<AgentResponse> GetAgentChatResponseAsync(List<AgentMessage> history, string cartId);
    Task<IReadOnlyList<Product>> SearchByNaturalLanguageAsync(string query, int count = 10);
    Task<string> GetProductComparisonAsync(int productId1, int productId2);
    Task<string> GetProductExplanationAsync(int productId);
}

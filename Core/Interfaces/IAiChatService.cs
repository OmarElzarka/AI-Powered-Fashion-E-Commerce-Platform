using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IAiChatService
    {
        Task<string> GetChatResponseAsync(string userMessage);
    }
}

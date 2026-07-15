using System.Threading.Tasks;

namespace Core.Interfaces;

public interface ITextEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
}

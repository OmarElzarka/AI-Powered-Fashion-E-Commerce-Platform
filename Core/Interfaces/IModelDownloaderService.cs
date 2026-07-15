using System.Threading.Tasks;

namespace Core.Interfaces;

public interface IModelDownloaderService
{
    Task EnsureModelsDownloadedAsync(string modelsDirectory);
}

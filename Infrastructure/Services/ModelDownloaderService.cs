using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ModelDownloaderService : IModelDownloaderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ModelDownloaderService> _logger;

    public ModelDownloaderService(HttpClient httpClient, ILogger<ModelDownloaderService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task EnsureModelsDownloadedAsync(string modelsDirectory)
    {
        if (!Directory.Exists(modelsDirectory))
        {
            Directory.CreateDirectory(modelsDirectory);
        }

        var modelPath = Path.Combine(modelsDirectory, "model.onnx");
        var vocabPath = Path.Combine(modelsDirectory, "vocab.txt");

        // We use Xenova's quantized all-MiniLM-L6-v2 which is standard for web/local inference (~22MB)
        const string modelUrl = "https://huggingface.co/Xenova/all-MiniLM-L6-v2/resolve/main/onnx/model_quantized.onnx";
        const string vocabUrl = "https://huggingface.co/Xenova/all-MiniLM-L6-v2/resolve/main/vocab.txt";

        await DownloadFileIfNotExistsAsync(modelUrl, modelPath);
        await DownloadFileIfNotExistsAsync(vocabUrl, vocabPath);
    }

    private async Task DownloadFileIfNotExistsAsync(string url, string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            var fileInfo = new FileInfo(destinationPath);
            if (fileInfo.Length > 1024 * 1024) // At least 1MB
            {
                return;
            }
            _logger.LogWarning("{FileName} exists but is too small ({Size} bytes). Deleting and redownloading...", Path.GetFileName(destinationPath), fileInfo.Length);
            File.Delete(destinationPath);
        }

        _logger.LogInformation("Downloading {FileName} from {Url}...", Path.GetFileName(destinationPath), url);

        try
        {
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs);

            _logger.LogInformation("Successfully downloaded {FileName}.", Path.GetFileName(destinationPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download {FileName}", Path.GetFileName(destinationPath));
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
            throw;
        }
    }
}

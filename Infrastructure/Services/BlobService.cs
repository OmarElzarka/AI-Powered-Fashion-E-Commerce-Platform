using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public BlobService(IConfiguration config)
        {
            var connectionString = config.GetConnectionString("AzureBlobStorage");
            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerName = config["AzureBlobStorage:ContainerName"] ?? "images";
        }

        public async Task<string> UploadBlobAsync(string blobName, Stream content, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(blobName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            };

            await blobClient.UploadAsync(content, options);

            return blobClient.Uri.ToString();
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<bool> BlobExistsAsync(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            return await blobClient.ExistsAsync();
        }
    }
}

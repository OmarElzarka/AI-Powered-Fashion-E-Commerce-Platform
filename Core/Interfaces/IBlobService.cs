using System.IO;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IBlobService
    {
        Task<string> UploadBlobAsync(string blobName, Stream content, string contentType);
        Task DeleteBlobAsync(string blobName);
        Task<bool> BlobExistsAsync(string blobName);
    }
}

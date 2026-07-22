using System;
using System.Reflection;
using System.Text.Json;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

public class StoreContextSeed
{
    public static async Task SeedAsync(StoreContext context, UserManager<AppUser> userManager,
        IDataImportService dataImportService, IResponseCacheService responseCacheService, ILogger logger, string seedDataPath, IBlobService blobService, Microsoft.Extensions.Configuration.IConfiguration config)
    {
        if (!userManager.Users.Any(x => x.UserName == "admin@test.com"))
        {
            var user = new AppUser
            {
                UserName = "admin@test.com",
                Email = "admin@test.com"
            };

            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, "Admin");
        }

        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (!context.DeliveryMethods.Any())
        {
            var dmData = await File.ReadAllTextAsync(path + @"/Data/SeedData/delivery.json");
            var methods = JsonSerializer.Deserialize<List<DeliveryMethod>>(dmData);

            if (methods == null) return;

            context.DeliveryMethods.AddRange(methods);

            await context.SaveChangesAsync();
        }

        // Check if we need to run a full migration/purge
        var productCount = await context.Products.CountAsync();
        if (productCount != 5000)
        {
            logger.LogInformation("Product count is {Count}, expected 5000. Purging database for fresh seed...", productCount);
            
            // Delete all existing products and embeddings
            await context.ProductEmbeddings.ExecuteDeleteAsync();
            await context.Products.ExecuteDeleteAsync();
            
            var useBlobStorage = !string.IsNullOrEmpty(config.GetConnectionString("AzureBlobStorage"));
            
            var datasetPath = seedDataPath;
            var targetImagePath = Path.Combine(seedDataPath, "assets", "products");

            var result = await dataImportService.ImportFromDatasetAsync(datasetPath, targetImagePath);

            logger.LogInformation("Fashion dataset import complete: {Imported} imported, {Skipped} skipped, {Failed} failed ({Duration})",
                result.Imported, result.Skipped, result.Failed, result.Duration);

            if (useBlobStorage)
            {
                logger.LogInformation("Azure Blob Storage detected. Starting bulk image upload in background...");
                var imageDirectory = Path.Combine(seedDataPath, "images");
                if (Directory.Exists(imageDirectory))
                {
                    var imageFiles = Directory.GetFiles(imageDirectory, "*.jpg");
                    var semaphore = new SemaphoreSlim(10); // Upload 10 images concurrently
                    
                    var uploadTasks = imageFiles.Select(async filePath => 
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            var fileName = Path.GetFileName(filePath);
                            string blobUrl;
                            if (!await blobService.BlobExistsAsync(fileName))
                            {
                                await using var stream = File.OpenRead(filePath);
                                blobUrl = await blobService.UploadBlobAsync(fileName, stream, "image/jpeg");
                            }
                            else
                            {
                                // If exists, construct the URL (assuming standard format)
                                var connectionString = config.GetConnectionString("AzureBlobStorage");
                                var containerName = config["AzureBlobStorage:ContainerName"] ?? "images";
                                // Simplified URL extraction for already uploaded blobs
                                var accountName = connectionString.Split(';')
                                    .FirstOrDefault(x => x.StartsWith("AccountName="))?.Split('=')[1];
                                blobUrl = $"https://{accountName}.blob.core.windows.net/{containerName}/{fileName}";
                            }

                            // Update product in DB
                            if (int.TryParse(Path.GetFileNameWithoutExtension(fileName), out int productId))
                            {
                                await context.Products.Where(p => p.Id == productId)
                                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.ImageUrl, blobUrl));
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    _ = Task.WhenAll(uploadTasks).ContinueWith(t => 
                    {
                        if (t.IsFaulted) logger.LogError(t.Exception, "Error during bulk image upload");
                        else logger.LogInformation("Bulk image upload to Azure Blob Storage completed successfully.");
                    });
                }
            }

            if (result.Errors.Any())
            {
                foreach (var error in result.Errors.Take(10))
                {
                    logger.LogWarning("Import issue: {Error}", error);
                }
            }

            // Clear Redis cache so any cached empty results from before seeding are removed
            logger.LogInformation("Clearing product cache...");
            await responseCacheService.RemoveCacheByPattern("api/products|");
        }
    }
}

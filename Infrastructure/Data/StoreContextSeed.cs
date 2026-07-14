using System;
using System.Reflection;
using System.Text.Json;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

public class StoreContextSeed
{
    public static async Task SeedAsync(StoreContext context, UserManager<AppUser> userManager,
        IDataImportService dataImportService, IResponseCacheService responseCacheService, ILogger logger, string seedDataPath)
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

        // Import fashion dataset if no products exist
        if (!context.Products.Any())
        {
            logger.LogInformation("No products found. Starting fashion dataset import...");

            var datasetPath = seedDataPath;
            var targetImagePath = Path.Combine(seedDataPath, "assets", "products");

            var result = await dataImportService.ImportFromDatasetAsync(datasetPath, targetImagePath);

            logger.LogInformation("Fashion dataset import complete: {Imported} imported, {Skipped} skipped, {Failed} failed ({Duration})",
                result.Imported, result.Skipped, result.Failed, result.Duration);

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

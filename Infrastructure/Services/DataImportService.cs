using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DataImportService(StoreContext context, ILogger<DataImportService> logger) : IDataImportService
{
    public async Task<DataImportResult> ImportFromDatasetAsync(string datasetBasePath, string targetImagePath)
    {
        var sw = Stopwatch.StartNew();
        var result = new DataImportResult();

        try
        {
            var stylesPath = Path.Combine(datasetBasePath, "styles.csv");
            var stylesDir = Path.Combine(datasetBasePath, "styles");
            var imagesDir = Path.Combine(datasetBasePath, "images");

            if (!File.Exists(stylesPath))
            {
                result.Errors.Add($"styles.csv not found at {stylesPath}");
                return result;
            }

            // Ensure target image directory exists
            Directory.CreateDirectory(targetImagePath);

            // Get existing product IDs to avoid duplicates
            var existingIds = await context.Products.Select(p => p.Id).ToHashSetAsync();

            // Read CSV lines
            var lines = await File.ReadAllLinesAsync(stylesPath);
            var products = new List<Product>();
            var random = new Random(42); // Deterministic seed for reproducibility

            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    result.TotalProcessed++;
                    var row = ParseCsvLine(lines[i]);

                    if (row.Length < 10)
                    {
                        result.Failed++;
                        result.Errors.Add($"Line {i}: insufficient columns ({row.Length})");
                        continue;
                    }

                    if (!int.TryParse(row[0], out var id))
                    {
                        result.Failed++;
                        continue;
                    }

                    if (existingIds.Contains(id))
                    {
                        result.Skipped++;
                        continue;
                    }

                    // Check if image exists
                    var sourceImage = Path.Combine(imagesDir, $"{id}.jpg");
                    if (!File.Exists(sourceImage))
                    {
                        result.Skipped++;
                        continue;
                    }

                    // Copy image to target
                    var targetImage = Path.Combine(targetImagePath, $"{id}.jpg");
                    if (!File.Exists(targetImage))
                    {
                        File.Copy(sourceImage, targetImage, false);
                    }

                    // Parse JSON for enriched metadata
                    var jsonPath = Path.Combine(stylesDir, $"{id}.json");
                    var jsonData = await ReadJsonMetadata(jsonPath);

                    // Build product from CSV + JSON
                    var product = new Product
                    {
                        Id = id,
                        Name = CleanString(row[9]), // productDisplayName
                        Description = jsonData.Description,
                        Brand = jsonData.BrandName ?? CleanString(ExtractBrandFromName(row[9])),
                        Category = CleanString(row[2]), // masterCategory
                        SubCategory = CleanString(row[3]),
                        ArticleType = CleanString(row[4]),
                        Gender = MapGender(CleanString(row[1])),
                        AgeGroup = jsonData.AgeGroup ?? "",
                        BaseColor = CleanString(row[5]),
                        Season = CleanString(row[6]),
                        Usage = CleanString(row[8]),
                        Material = jsonData.Material,
                        Pattern = jsonData.Pattern,
                        Fit = jsonData.Fit,
                        StyleType = jsonData.StyleType,
                        FashionType = jsonData.FashionType,
                        Year = ParseYear(row[7]),
                        Price = jsonData.Price > 0 ? jsonData.Price : GeneratePrice(row[2], random),
                        DiscountPercentage = jsonData.DiscountPercent,
                        Rating = jsonData.Rating > 0 ? jsonData.Rating : GenerateRating(random),
                        ImageUrl = $"/assets/products/{id}.jpg",
                        FrontImageUrl = "", // Not stored locally
                        BackImageUrl = "",
                        SearchImageUrl = "",
                        Tags = GenerateTags(row),
                        IsFeatured = random.NextDouble() > 0.85, // ~15% featured
                        IsNewArrival = random.NextDouble() > 0.80, // ~20% new arrivals
                        QuantityInStock = random.Next(5, 100),
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                        UpdatedAt = DateTime.UtcNow
                    };

                    products.Add(product);
                    result.Imported++;

                    // Batch insert every 500
                    if (products.Count >= 500)
                    {
                        await BatchInsert(products);
                        products.Clear();
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    if (result.Errors.Count < 50) // Cap error messages
                    {
                        result.Errors.Add($"Line {i}: {ex.Message}");
                    }
                }
            }

            // Insert remaining
            if (products.Count > 0)
            {
                await BatchInsert(products);
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Fatal: {ex.Message}");
            logger.LogError(ex, "Data import failed");
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        logger.LogInformation("Data import completed: {Imported} imported, {Skipped} skipped, {Failed} failed in {Duration}",
            result.Imported, result.Skipped, result.Failed, result.Duration);

        return result;
    }

    private async Task BatchInsert(List<Product> products)
    {
        // Use identity insert for explicit IDs
        await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Products ON");
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Products OFF");

        // Detach all to free memory
        foreach (var entity in context.ChangeTracker.Entries().ToList())
        {
            entity.State = EntityState.Detached;
        }
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (line[i] == ',' && !inQuotes)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(line[i]);
            }
        }
        fields.Add(current.ToString().Trim());
        return fields.ToArray();
    }

    private async Task<JsonMetadata> ReadJsonMetadata(string jsonPath)
    {
        var meta = new JsonMetadata();

        if (!File.Exists(jsonPath)) return meta;

        try
        {
            var json = await File.ReadAllTextAsync(jsonPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data))
            {
                meta.BrandName = GetStringOrDefault(data, "brandName");
                meta.AgeGroup = GetStringOrDefault(data, "ageGroup");
                meta.StyleType = GetStringOrDefault(data, "styleType");
                meta.FashionType = GetStringOrDefault(data, "fashionType");

                if (data.TryGetProperty("price", out var priceProp) && priceProp.ValueKind == JsonValueKind.Number)
                {
                    meta.Price = priceProp.GetDecimal();
                }

                if (data.TryGetProperty("myntraRating", out var ratingProp) && ratingProp.ValueKind == JsonValueKind.Number)
                {
                    meta.Rating = ratingProp.GetDouble();
                    if (meta.Rating <= 0 || meta.Rating > 5) meta.Rating = 0;
                }

                if (data.TryGetProperty("discountData", out var discountData) &&
                    discountData.TryGetProperty("discountPercent", out var discountPct) &&
                    discountPct.ValueKind == JsonValueKind.Number)
                {
                    meta.DiscountPercent = discountPct.GetDecimal();
                }

                // Article attributes
                if (data.TryGetProperty("articleAttributes", out var attrs))
                {
                    meta.Pattern = GetStringOrDefault(attrs, "Pattern");
                    meta.Material = GetStringOrDefault(attrs, "Fabric");
                    meta.Fit = GetStringOrDefault(attrs, "Shape");
                }

                // Description
                if (data.TryGetProperty("productDescriptors", out var descriptors) &&
                    descriptors.TryGetProperty("description", out var desc) &&
                    desc.TryGetProperty("value", out var descValue))
                {
                    meta.Description = StripHtml(descValue.GetString() ?? "");
                }
            }
        }
        catch
        {
            // Silently skip malformed JSON
        }

        return meta;
    }

    private static string GetStringOrDefault(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString() ?? "";
        }
        return "";
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    private static string CleanString(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }

    private static string MapGender(string gender)
    {
        return gender switch
        {
            "Men" => "Men",
            "Women" => "Women",
            "Boys" => "Boys",
            "Girls" => "Girls",
            "Unisex" => "Unisex",
            _ => gender
        };
    }

    private static int ParseYear(string yearStr)
    {
        if (decimal.TryParse(yearStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var yearDec))
        {
            return (int)yearDec;
        }
        return 2020;
    }

    private static string ExtractBrandFromName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";
        var parts = name.Split(' ');
        return parts.Length > 0 ? parts[0] : "Unknown";
    }

    private static decimal GeneratePrice(string category, Random random)
    {
        return category switch
        {
            "Accessories" => Math.Round((decimal)(random.NextDouble() * 50 + 10), 2),
            "Footwear" => Math.Round((decimal)(random.NextDouble() * 150 + 30), 2),
            "Apparel" => Math.Round((decimal)(random.NextDouble() * 200 + 20), 2),
            _ => Math.Round((decimal)(random.NextDouble() * 100 + 15), 2)
        };
    }

    private static double GenerateRating(Random random)
    {
        return Math.Round(random.NextDouble() * 2 + 3, 1); // 3.0 to 5.0
    }

    private static string GenerateTags(string[] row)
    {
        var tags = new List<string>();
        if (row.Length > 2 && !string.IsNullOrEmpty(row[2])) tags.Add(row[2]); // category
        if (row.Length > 4 && !string.IsNullOrEmpty(row[4])) tags.Add(row[4]); // articleType
        if (row.Length > 5 && !string.IsNullOrEmpty(row[5])) tags.Add(row[5]); // color
        if (row.Length > 6 && !string.IsNullOrEmpty(row[6])) tags.Add(row[6]); // season
        if (row.Length > 8 && !string.IsNullOrEmpty(row[8])) tags.Add(row[8]); // usage
        return string.Join(",", tags.Where(t => !string.IsNullOrWhiteSpace(t)));
    }

    private class JsonMetadata
    {
        public string BrandName { get; set; } = "";
        public string AgeGroup { get; set; } = "";
        public string StyleType { get; set; } = "";
        public string FashionType { get; set; } = "";
        public decimal Price { get; set; }
        public double Rating { get; set; }
        public decimal DiscountPercent { get; set; }
        public string Pattern { get; set; } = "";
        public string Material { get; set; } = "";
        public string Fit { get; set; } = "";
        public string Description { get; set; } = "";
    }
}

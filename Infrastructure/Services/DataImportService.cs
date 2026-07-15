using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DataImportService(StoreContext context, ILogger<DataImportService> logger, ITextEmbeddingService textEmbeddingService) : IDataImportService
{
    // CSV column indices for products_for_ai.csv
    private const int COL_ID = 0;
    private const int COL_IMAGE_PATH = 1;
    private const int COL_JSON_PATH = 2;
    private const int COL_PRODUCT_NAME = 3;
    private const int COL_BRAND = 4;
    private const int COL_MASTER_CATEGORY = 5;
    private const int COL_SUBCATEGORY = 6;
    private const int COL_ARTICLE_TYPE = 7;
    private const int COL_GENDER = 8;
    private const int COL_AGE_GROUP = 9;
    private const int COL_BASE_COLOR = 10;
    private const int COL_SEASON = 11;
    private const int COL_USAGE = 12;
    private const int COL_YEAR = 13;
    private const int COL_PATTERN = 14;
    private const int COL_MATERIAL = 15;
    private const int COL_FIT = 16;
    private const int COL_NECK = 17;
    private const int COL_SLEEVE = 18;
    private const int COL_OCCASION = 19;
    private const int COL_PRICE = 20;
    private const int COL_DISCOUNTED_PRICE = 21;
    private const int COL_FASHION_TYPE = 22;
    private const int COL_DISPLAY_CATEGORIES = 23;
    private const int COL_DESCRIPTION = 24;
    private const int COL_SEARCH_TEXT = 25;
    private const int EXPECTED_COLUMNS = 26;

    public async Task<DataImportResult> ImportFromDatasetAsync(string datasetBasePath, string targetImagePath)
    {
        var sw = Stopwatch.StartNew();
        var result = new DataImportResult();

        try
        {
            var csvPath = Path.Combine(datasetBasePath, "products_for_ai.csv");

            if (!File.Exists(csvPath))
            {
                logger.LogWarning("products_for_ai.csv not found at {Path}, trying styles.csv fallback", csvPath);
                result.Errors.Add($"products_for_ai.csv not found at {csvPath}");
                return result;
            }

            logger.LogInformation("Starting import from {Path}", csvPath);

            // Get existing product IDs to avoid duplicates
            var existingIds = await context.Products.Select(p => p.Id).ToHashSetAsync();

            // Read and parse CSV
            var csvContent = await File.ReadAllTextAsync(csvPath);
            var records = ParseCsv(csvContent);
            var products = new List<Product>();
            var embeddings = new List<ProductEmbedding>();
            var random = new Random(42); // Deterministic seed for reproducibility

            // Skip header (index 0)
            for (int i = 1; i < records.Count; i++)
            {
                try
                {
                    result.TotalProcessed++;
                    var row = records[i];

                    if (row.Length < EXPECTED_COLUMNS)
                    {
                        result.Failed++;
                        if (result.Errors.Count < 50)
                            result.Errors.Add($"Record {i}: insufficient columns ({row.Length}, expected {EXPECTED_COLUMNS})");
                        continue;
                    }

                    if (!int.TryParse(row[COL_ID], out var id))
                    {
                        result.Failed++;
                        if (result.Errors.Count < 50)
                            result.Errors.Add($"Record {i}: invalid ID '{row[COL_ID]}'");
                        continue;
                    }

                    if (existingIds.Contains(id))
                    {
                        result.Skipped++;
                        continue;
                    }

                    var productName = Clean(row[COL_PRODUCT_NAME]);
                    if (string.IsNullOrEmpty(productName))
                    {
                        result.Failed++;
                        continue;
                    }

                    // Parse price fields
                    var price = ParseDecimal(row[COL_PRICE]);
                    var discountedPrice = ParseDecimal(row[COL_DISCOUNTED_PRICE]);

                    // Calculate discount percentage
                    decimal discountPercentage = 0;
                    if (price > 0 && discountedPrice > 0 && discountedPrice < price)
                    {
                        discountPercentage = Math.Round((price - discountedPrice) / price * 100, 2);
                    }

                    // Build tags from display_categories + article type + color + season
                    var tags = BuildTags(row);

                    var product = new Product
                    {
                        Id = id,
                        Name = productName,
                        Description = CleanDescription(row[COL_DESCRIPTION]),
                        Brand = Clean(row[COL_BRAND]),
                        Category = Clean(row[COL_MASTER_CATEGORY]),
                        SubCategory = Clean(row[COL_SUBCATEGORY]),
                        ArticleType = Clean(row[COL_ARTICLE_TYPE]),
                        Gender = MapGender(Clean(row[COL_GENDER])),
                        AgeGroup = Clean(row[COL_AGE_GROUP]),
                        BaseColor = Clean(row[COL_BASE_COLOR]),
                        Season = Clean(row[COL_SEASON]),
                        Usage = Clean(row[COL_USAGE]),
                        Material = Clean(row[COL_MATERIAL]),
                        Pattern = Clean(row[COL_PATTERN]),
                        Fit = Clean(row[COL_FIT]),
                        Neck = Clean(row[COL_NECK]),
                        Sleeve = Clean(row[COL_SLEEVE]),
                        StyleType = Clean(row[COL_OCCASION]),
                        FashionType = Clean(row[COL_FASHION_TYPE]),
                        Year = ParseYear(row[COL_YEAR]),
                        Price = price > 0 ? price : GeneratePrice(Clean(row[COL_MASTER_CATEGORY]), random),
                        DiscountPercentage = discountPercentage,
                        Rating = GenerateRating(random),
                        ImageUrl = $"/images/{id}.jpg",
                        FrontImageUrl = "",
                        BackImageUrl = "",
                        SearchImageUrl = "",
                        Tags = tags,
                        IsFeatured = random.NextDouble() > 0.85, // ~15% featured
                        IsNewArrival = random.NextDouble() > 0.80, // ~20% new arrivals
                        QuantityInStock = random.Next(5, 100),
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                        UpdatedAt = DateTime.UtcNow
                    };

                    products.Add(product);

                    var semanticString = Clean(row[COL_SEARCH_TEXT]);
                    if (string.IsNullOrEmpty(semanticString))
                    {
                        semanticString = $"{product.Name}. {product.Description} Brand: {product.Brand}. Category: {product.Category} {product.SubCategory} {product.ArticleType}. Gender: {product.Gender}. Color: {product.BaseColor}. Pattern: {product.Pattern}. Fit: {product.Fit}. Material: {product.Material}. Occasion: {product.StyleType}. Usage: {product.Usage}. Tags: {product.Tags}";
                    }
                    var vector = await textEmbeddingService.GenerateEmbeddingAsync(semanticString);
                    var embeddingJson = JsonSerializer.Serialize(vector);
                    
                    embeddings.Add(new ProductEmbedding
                    {
                        ProductId = product.Id,
                        VectorJson = embeddingJson
                    });

                    result.Imported++;

                    // Batch insert every 500
                    if (products.Count >= 500)
                    {
                        await BatchInsert(products, embeddings);
                        products.Clear();
                        embeddings.Clear();
                        logger.LogInformation("Batch inserted. Progress: {Imported} imported so far", result.Imported);
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    if (result.Errors.Count < 50)
                    {
                        result.Errors.Add($"Record {i}: {ex.Message}");
                    }
                }
            }

            // Insert remaining
            if (products.Count > 0)
            {
                await BatchInsert(products, embeddings);
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

    private async Task BatchInsert(List<Product> products, List<ProductEmbedding> embeddings)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Use identity insert for explicit IDs
            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Products ON");
            context.Products.AddRange(products);
            context.ProductEmbeddings.AddRange(embeddings);
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Products OFF");
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        // Detach all to free memory
        foreach (var entity in context.ChangeTracker.Entries().ToList())
        {
            entity.State = EntityState.Detached;
        }
    }

    /// <summary>
    /// Parses a full CSV string into a list of string arrays, handling quoted fields
    /// that may contain commas, newlines, and escaped quotes.
    /// </summary>
    private static List<string[]> ParseCsv(string content)
    {
        var records = new List<string[]>();
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        int i = 0;

        while (i < content.Length)
        {
            char c = content[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped quote ""
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        current.Append('"');
                        i += 2;
                        continue;
                    }
                    else
                    {
                        inQuotes = false;
                        i++;
                        continue;
                    }
                }
                else
                {
                    current.Append(c);
                    i++;
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                    i++;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString().Trim());
                    current.Clear();
                    i++;
                }
                else if (c == '\n' || (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n'))
                {
                    fields.Add(current.ToString().Trim());
                    current.Clear();
                    records.Add(fields.ToArray());
                    fields.Clear();

                    if (c == '\r') i += 2; // skip \r\n
                    else i++; // skip \n
                }
                else if (c == '\r')
                {
                    // standalone \r
                    fields.Add(current.ToString().Trim());
                    current.Clear();
                    records.Add(fields.ToArray());
                    fields.Clear();
                    i++;
                }
                else
                {
                    current.Append(c);
                    i++;
                }
            }
        }

        // Handle last field/record
        if (current.Length > 0 || fields.Count > 0)
        {
            fields.Add(current.ToString().Trim());
            records.Add(fields.ToArray());
        }

        return records;
    }

    private static string Clean(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("nan", StringComparison.OrdinalIgnoreCase))
            return "";
        return value.Trim();
    }

    private static string CleanDescription(string value)
    {
        var cleaned = Clean(value);
        if (string.IsNullOrEmpty(cleaned)) return "";

        // Strip any residual HTML tags
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "<.*?>", " ");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");

        // Truncate to fit DB column (2000 chars max)
        if (cleaned.Length > 1900)
            cleaned = cleaned[..1900] + "...";

        return cleaned.Trim();
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

    private static decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("nan", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        return 0;
    }

    private static decimal GeneratePrice(string category, Random random)
    {
        return category switch
        {
            "Accessories" => Math.Round((decimal)(random.NextDouble() * 50 + 10), 2),
            "Footwear" => Math.Round((decimal)(random.NextDouble() * 150 + 30), 2),
            "Apparel" => Math.Round((decimal)(random.NextDouble() * 200 + 20), 2),
            "Personal Care" => Math.Round((decimal)(random.NextDouble() * 40 + 5), 2),
            _ => Math.Round((decimal)(random.NextDouble() * 100 + 15), 2)
        };
    }

    private static double GenerateRating(Random random)
    {
        return Math.Round(random.NextDouble() * 2 + 3, 1); // 3.0 to 5.0
    }

    private static string BuildTags(string[] row)
    {
        var tags = new List<string>();

        // Add display_categories (may be comma-separated already like "Sports Wear,Winterwear")
        var displayCats = Clean(row[COL_DISPLAY_CATEGORIES]);
        if (!string.IsNullOrEmpty(displayCats))
        {
            tags.AddRange(displayCats.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim()));
        }

        // Add article type, color, season, usage as tags
        var articleType = Clean(row[COL_ARTICLE_TYPE]);
        if (!string.IsNullOrEmpty(articleType) && !tags.Contains(articleType))
            tags.Add(articleType);

        var color = Clean(row[COL_BASE_COLOR]);
        if (!string.IsNullOrEmpty(color) && !tags.Contains(color))
            tags.Add(color);

        var season = Clean(row[COL_SEASON]);
        if (!string.IsNullOrEmpty(season) && !tags.Contains(season))
            tags.Add(season);

        var usage = Clean(row[COL_USAGE]);
        if (!string.IsNullOrEmpty(usage) && !tags.Contains(usage))
            tags.Add(usage);

        var result = string.Join(",", tags.Where(t => !string.IsNullOrWhiteSpace(t)));

        // Truncate to fit DB column (1000 chars max)
        if (result.Length > 990)
            result = result[..990];

        return result;
    }
}

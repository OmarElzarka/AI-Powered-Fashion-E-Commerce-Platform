using API.Middleware;
using API.SignalR;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text;
using Infrastructure.Plugins;
using Microsoft.SemanticKernel;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<StoreContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Repository & UnitOfWork
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Business Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IDataImportService, DataImportService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddHttpClient<IModelDownloaderService, ModelDownloaderService>();
builder.Services.AddSingleton<ITextEmbeddingService>(sp => 
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var modelPath = Path.Combine(env.ContentRootPath, "assets", "models", "model.onnx");
    var vocabPath = Path.Combine(env.ContentRootPath, "assets", "models", "vocab.txt");
    return new TextEmbeddingService(modelPath, vocabPath);
});

// AI Services (future-ready)
builder.Services.AddSingleton<RecommendationService>();
builder.Services.AddSingleton<IRecommendationService>(sp => sp.GetRequiredService<RecommendationService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<RecommendationService>());

builder.Services.AddScoped<IAIRecommendationService, AIRecommendationService>();
builder.Services.AddScoped<IAIStylistService, AIStylistService>();
builder.Services.AddScoped<IAIShoppingAgentService, AIShoppingAgentService>();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<ShoppingAgentPlugin>();
builder.Services.AddScoped<Infrastructure.Services.AgentResponseContext>();

var geminiKey = builder.Configuration["Gemini:ApiKey"];
if (!string.IsNullOrEmpty(geminiKey))
{
    builder.Services.AddKernel()
        .AddGoogleAIGeminiChatCompletion(
            modelId: "gemini-3.1-flash-lite",
            apiKey: geminiKey,
            apiVersion: Microsoft.SemanticKernel.Connectors.Google.GoogleAIVersion.V1_Beta
        );
}

builder.Services.AddCors();
builder.Services.AddSingleton<IConnectionMultiplexer>(config =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis")
        ?? throw new Exception("Cannot get redis connection string");
    var configuation = ConfigurationOptions.Parse(connectionString, true);
    return ConnectionMultiplexer.Connect(configuation);
});
builder.Services.AddSingleton<ICartService, CartService>();
builder.Services.AddSingleton<IResponseCacheService, ResponseCacheService>();

builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<AppUser>(opt =>
{
    opt.SignIn.RequireConfirmedAccount = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<StoreContext>();

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Token:Key"]!)),
            ValidIssuer = builder.Configuration["Token:Issuer"],
            ValidateIssuer = true,
            ValidateAudience = false
        };
    });
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(x => x
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithOrigins("http://localhost:4200", "https://localhost:4200"));

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Data", "SeedData", "images")),
    RequestPath = "/images"
});

app.MapControllers();
app.MapGroup("api").MapIdentityApi<AppUser>();
app.MapHub<NotificationHub>("/hub/notifications");

try
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<StoreContext>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var cacheService = services.GetRequiredService<IResponseCacheService>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    var modelDownloader = services.GetRequiredService<IModelDownloaderService>();
    await modelDownloader.EnsureModelsDownloadedAsync(Path.Combine(builder.Environment.ContentRootPath, "assets", "models"));

    var dataImportService = services.GetRequiredService<IDataImportService>();

    await context.Database.MigrateAsync();
    await StoreContextSeed.SeedAsync(context, userManager, dataImportService, cacheService, logger,
        Path.Combine(builder.Environment.ContentRootPath, "Data", "SeedData"));
}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}

app.Run();

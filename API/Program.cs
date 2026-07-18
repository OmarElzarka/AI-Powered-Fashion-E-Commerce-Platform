using API.Middleware;
using API.SignalR;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Infrastructure.Plugins;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using System.Text;
using Serilog;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/api-log-.txt", rollingInterval: RollingInterval.Day));

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
});
// Add services to the container.

builder.Services.AddControllers(options => 
{
    options.Filters.Add<API.Filters.StripeExceptionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", null, null),
            new List<string>()
        }
    });
});
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
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<INotificationService, API.SignalR.NotificationService>();
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

app.UseSerilogRequestLogging();
app.UseHttpLogging();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

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

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Core.Entities;
using Core.Interfaces;
using FluentAssertions;
using Infrastructure.Plugins;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using Xunit;

namespace Tests.Unit.Services;

public class AIShoppingAgentServiceTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ICartService> _mockCartService;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IChatCompletionService> _mockChatService;
    private readonly Mock<AgentResponseContext> _mockAgentContext;
    private readonly ShoppingAgentPlugin _plugin;
    private readonly Kernel _kernel;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<AIShoppingAgentService>> _mockLogger;
    private readonly AIShoppingAgentService _sut;

    public AIShoppingAgentServiceTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _mockCartService = _fixture.Freeze<Mock<ICartService>>();
        _mockProductRepository = _fixture.Freeze<Mock<IProductRepository>>();
        _mockChatService = _fixture.Freeze<Mock<IChatCompletionService>>();
        
        _mockAgentContext = new Mock<AgentResponseContext>();
        
        // Setup minimal Kernel
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(_mockChatService.Object);
        _kernel = builder.Build();

        var mockTextEmbedding = new Mock<ITextEmbeddingService>();
        var mockRecommendation = new Mock<IRecommendationService>();
        
        _plugin = new ShoppingAgentPlugin(
            _mockProductRepository.Object,
            _mockCartService.Object,
            mockTextEmbedding.Object,
            mockRecommendation.Object,
            _mockAgentContext.Object
        );

        _mockLogger = _fixture.Freeze<Mock<Microsoft.Extensions.Logging.ILogger<AIShoppingAgentService>>>();

        _sut = new AIShoppingAgentService(
            _mockCartService.Object,
            _mockProductRepository.Object,
            _mockChatService.Object,
            _kernel,
            _plugin,
            _mockAgentContext.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ConfirmActionAsync_AddToCart_WhenProductExists_AddsToCartAndReturnsUpdatedCart()
    {
        // Arrange
        var product = _fixture.Create<Product>();
        _mockProductRepository.Setup(x => x.GetProductByIdAsync(product.Id)).ReturnsAsync(product);

        var cart = new ShoppingCart { Id = "cart_123" };
        _mockCartService.Setup(x => x.GetCartAsync("cart_123")).ReturnsAsync(cart);
        _mockCartService.Setup(x => x.SetCartAsync(It.IsAny<ShoppingCart>())).ReturnsAsync((ShoppingCart c) => c);

        var parameters = new Dictionary<string, object>
        {
            { "productId", JsonSerializer.SerializeToElement(product.Id) },
            { "quantity", JsonSerializer.SerializeToElement(2) }
        };

        var confirmation = new ActionConfirmation
        {
            Action = "AddToCart",
            Parameters = parameters
        };

        // Act
        var result = await _sut.ConfirmActionAsync(confirmation, "cart_123");

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        result.Items[0].ProductId.Should().Be(product.Id);
        result.Items[0].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task ConfirmActionAsync_RemoveFromCart_WhenItemExists_RemovesFromCart()
    {
        // Arrange
        var cart = new ShoppingCart { Id = "cart_123" };
        cart.Items.Add(new CartItem { ProductId = 1, Quantity = 2, ProductName = "T", PictureUrl = "T", Brand = "T", Type = "T" });

        _mockCartService.Setup(x => x.GetCartAsync("cart_123")).ReturnsAsync(cart);
        _mockCartService.Setup(x => x.SetCartAsync(It.IsAny<ShoppingCart>())).ReturnsAsync((ShoppingCart c) => c);

        var parameters = new Dictionary<string, object>
        {
            { "productId", JsonSerializer.SerializeToElement(1) },
            { "quantity", JsonSerializer.SerializeToElement(2) }
        };

        var confirmation = new ActionConfirmation
        {
            Action = "RemoveFromCart",
            Parameters = parameters
        };

        // Act
        var result = await _sut.ConfirmActionAsync(confirmation, "cart_123");

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }
}

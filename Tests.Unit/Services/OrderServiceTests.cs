using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using FluentAssertions;
using Infrastructure.Services;
using Moq;
using Xunit;

namespace Tests.Unit.Services;

public class OrderServiceTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICartService> _mockCartService;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<OrderService>> _mockLogger;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _mockUnitOfWork = _fixture.Freeze<Mock<IUnitOfWork>>();
        _mockCartService = _fixture.Freeze<Mock<ICartService>>();
        _mockLogger = _fixture.Freeze<Mock<Microsoft.Extensions.Logging.ILogger<OrderService>>>();

        _sut = new OrderService(_mockCartService.Object, _mockUnitOfWork.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenCartDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockCartService.Setup(x => x.GetCartAsync(It.IsAny<string>()))
            .ReturnsAsync((ShoppingCart?)null);

        // Act
        var result = await _sut.CreateOrderAsync("test@test.com", 1, "cart_123", new ShippingAddress { Name = "T", Line1 = "T", City = "T", PostalCode = "T", Country = "T" }, new PaymentSummary { Brand = "T" }, 0);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrderAsync_WhenPaymentIntentIdIsNull_ReturnsNull()
    {
        // Arrange
        var cart = _fixture.Build<ShoppingCart>()
            .With(c => c.PaymentIntentId, (string?)null)
            .Create();

        _mockCartService.Setup(x => x.GetCartAsync(It.IsAny<string>()))
            .ReturnsAsync(cart);

        // Act
        var result = await _sut.CreateOrderAsync("test@test.com", 1, "cart_123", new ShippingAddress { Name = "T", Line1 = "T", City = "T", PostalCode = "T", Country = "T" }, new PaymentSummary { Brand = "T" }, 0);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrderAsync_WhenProductItemDoesNotExist_ReturnsNull()
    {
        // Arrange
        var cart = _fixture.Build<ShoppingCart>()
            .With(c => c.PaymentIntentId, "pi_123")
            .With(c => c.Items, [_fixture.Create<CartItem>()])
            .Create();

        _mockCartService.Setup(x => x.GetCartAsync(It.IsAny<string>()))
            .ReturnsAsync(cart);

        _mockUnitOfWork.Setup(x => x.Repository<Order>().GetEntityWithSpec(It.IsAny<ISpecification<Order>>()))
            .ReturnsAsync((Order?)null);

        _mockUnitOfWork.Setup(x => x.Repository<Product>().GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _sut.CreateOrderAsync("test@test.com", 1, "cart_123", new ShippingAddress { Name = "T", Line1 = "T", City = "T", PostalCode = "T", Country = "T" }, new PaymentSummary { Brand = "T" }, 0);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrderAsync_WhenValidDataProvided_CreatesAndReturnsOrder()
    {
        // Arrange
        var cartItem = _fixture.Create<CartItem>();
        var cart = _fixture.Build<ShoppingCart>()
            .With(c => c.PaymentIntentId, "pi_123")
            .With(c => c.Items, [cartItem])
            .Create();

        var product = _fixture.Create<Product>();
        var deliveryMethod = _fixture.Create<DeliveryMethod>();
        var shippingAddress = _fixture.Create<ShippingAddress>();
        var paymentSummary = _fixture.Create<PaymentSummary>();

        _mockCartService.Setup(x => x.GetCartAsync(It.IsAny<string>()))
            .ReturnsAsync(cart);

        _mockUnitOfWork.Setup(x => x.Repository<Order>().GetEntityWithSpec(It.IsAny<ISpecification<Order>>()))
            .ReturnsAsync((Order?)null);

        _mockUnitOfWork.Setup(x => x.Repository<Product>().GetByIdAsync(cartItem.ProductId))
            .ReturnsAsync(product);

        _mockUnitOfWork.Setup(x => x.Repository<DeliveryMethod>().GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(deliveryMethod);

        _mockUnitOfWork.Setup(x => x.Complete())
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateOrderAsync("test@test.com", deliveryMethod.Id, cart.Id, shippingAddress, paymentSummary, 10m);

        // Assert
        result.Should().NotBeNull();
        result!.BuyerEmail.Should().Be("test@test.com");
        result.PaymentIntentId.Should().Be("pi_123");
        result.Subtotal.Should().Be(product.Price * cartItem.Quantity);
        result.Discount.Should().Be(10m);
        
        _mockUnitOfWork.Verify(x => x.Repository<Order>().Add(It.IsAny<Order>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.Complete(), Times.Once);
    }
}

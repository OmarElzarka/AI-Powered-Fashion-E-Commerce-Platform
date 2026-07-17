using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Tests.Unit.Services;

public class PaymentServiceTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ICartService> _mockCartService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _mockCartService = _fixture.Freeze<Mock<ICartService>>();
        _mockUnitOfWork = _fixture.Freeze<Mock<IUnitOfWork>>();
        _mockNotificationService = _fixture.Freeze<Mock<INotificationService>>();
        
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(x => x["StripeSettings:SecretKey"]).Returns("sk_test_123");

        _sut = new PaymentService(_mockConfig.Object, _mockCartService.Object, _mockUnitOfWork.Object, _mockNotificationService.Object);
    }

    [Fact]
    public async Task CreateOrUpdatePaymentIntent_WhenCartNotFound_ThrowsException()
    {
        // Arrange
        _mockCartService.Setup(x => x.GetCartAsync(It.IsAny<string>()))
            .ReturnsAsync((ShoppingCart?)null);

        // Act
        var act = async () => await _sut.CreateOrUpdatePaymentIntent("cart_123");

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Cart unavailable");
    }

    [Fact]
    public async Task UpdateOrderPaymentSucceeded_WhenAmountsMatch_UpdatesStatusToPaymentReceived()
    {
        // Arrange
        var order = _fixture.Build<Core.Entities.OrderAggregate.Order>()
            .With(o => o.Subtotal, 100m)
            .With(o => o.DeliveryMethod, new Core.Entities.DeliveryMethod { Price = 10m, DeliveryTime = "T", ShortName = "T", Description = "T" })
            .With(o => o.Discount, 0m)
            .Create();
            
        var expectedTotalInCents = (long)Math.Round((100m + 10m) * 100, MidpointRounding.AwayFromZero);

        _mockUnitOfWork.Setup(x => x.Repository<Core.Entities.OrderAggregate.Order>().GetEntityWithSpec(It.IsAny<ISpecification<Core.Entities.OrderAggregate.Order>>()))
            .ReturnsAsync(order);

        // Act
        var result = await _sut.UpdateOrderPaymentSucceeded("pi_123", expectedTotalInCents);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(Core.Entities.OrderAggregate.OrderStatus.PaymentReceived);
        _mockUnitOfWork.Verify(x => x.Complete(), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderPaymentSucceeded_WhenAmountsMismatch_UpdatesStatusToPaymentMismatch()
    {
        // Arrange
        var order = _fixture.Build<Core.Entities.OrderAggregate.Order>()
            .With(o => o.Subtotal, 100m)
            .With(o => o.DeliveryMethod, new Core.Entities.DeliveryMethod { Price = 10m, DeliveryTime = "T", ShortName = "T", Description = "T" })
            .With(o => o.Discount, 0m)
            .Create();
            
        var wrongAmount = 9999L; // Different from actual total

        _mockUnitOfWork.Setup(x => x.Repository<Core.Entities.OrderAggregate.Order>().GetEntityWithSpec(It.IsAny<ISpecification<Core.Entities.OrderAggregate.Order>>()))
            .ReturnsAsync(order);

        // Act
        var result = await _sut.UpdateOrderPaymentSucceeded("pi_123", wrongAmount);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(Core.Entities.OrderAggregate.OrderStatus.PaymentMismatch);
        _mockUnitOfWork.Verify(x => x.Complete(), Times.Once);
    }
}

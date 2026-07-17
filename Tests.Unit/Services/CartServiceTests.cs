using System;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Core.Entities;
using FluentAssertions;
using Infrastructure.Services;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Tests.Unit.Services;

public class CartServiceTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IConnectionMultiplexer> _mockMultiplexer;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly CartService _sut;

    public CartServiceTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _mockDatabase = new Mock<IDatabase>();
        _mockMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        _sut = new CartService(_mockMultiplexer.Object);
    }

    [Fact]
    public async Task GetCartAsync_WhenCartExists_ReturnsDeserializedCart()
    {
        var expectedCart = _fixture.Create<ShoppingCart>();
        var json = JsonSerializer.Serialize(expectedCart);
        _mockDatabase.Setup(x => x.StringGetAsync(expectedCart.Id, It.IsAny<CommandFlags>()))
            .ReturnsAsync(json);

        // Act
        var result = await _sut.GetCartAsync(expectedCart.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedCart.Id);
    }

    [Fact]
    public async Task GetCartAsync_WhenCartDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockDatabase.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _sut.GetCartAsync("invalid_id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetCartAsync_WhenCalled_SavesToRedisAndReturnsCart()
    {
        // Arrange
        var cart = _fixture.Create<ShoppingCart>();
        _mockDatabase.Setup(x => x.StringSetAsync(cart.Id, It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
            
        var json = JsonSerializer.Serialize(cart);
        _mockDatabase.Setup(x => x.StringGetAsync(cart.Id, It.IsAny<CommandFlags>()))
            .ReturnsAsync(json);

        // Act
        var result = await _sut.SetCartAsync(cart);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cart);
        _mockDatabase.Verify(x => x.StringSetAsync(cart.Id, It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCartAsync_WhenCalled_ReturnsTrueIfDeleted()
    {
        // Arrange
        _mockDatabase.Setup(x => x.KeyDeleteAsync("cart_123", CommandFlags.None))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteCartAsync("cart_123");

        // Assert
        result.Should().BeTrue();
        _mockDatabase.Verify(x => x.KeyDeleteAsync("cart_123", CommandFlags.None), Times.Once);
    }
}

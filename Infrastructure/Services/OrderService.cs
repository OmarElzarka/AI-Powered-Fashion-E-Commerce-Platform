using System;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;

namespace Infrastructure.Services;

public class OrderService(ICartService cartService, IUnitOfWork unit) : IOrderService
{
    public async Task<Order?> CreateOrderAsync(string buyerEmail, int deliveryMethodId,
        string cartId, ShippingAddress shippingAddress, PaymentSummary paymentSummary,
        decimal discount)
    {
        var cart = await cartService.GetCartAsync(cartId);
        if (cart == null) return null;
        if (cart.PaymentIntentId == null) return null;

        var spec = new OrderSpecification(cart.PaymentIntentId, true);
        var existingOrder = await unit.Repository<Order>().GetEntityWithSpec(spec);

        if (existingOrder != null)
        {
            unit.Repository<Order>().Remove(existingOrder);
            await unit.Complete();
        }

        var items = new List<OrderItem>();

        foreach (var item in cart.Items)
        {
            var productItem = await unit.Repository<Product>().GetByIdAsync(item.ProductId);
            if (productItem == null) return null;

            var itemOrdered = new ProductItemOrdered
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                PictureUrl = item.PictureUrl
            };

            var orderItem = new OrderItem
            {
                ItemOrdered = itemOrdered,
                Price = productItem.Price,
                Quantity = item.Quantity
            };
            items.Add(orderItem);
        }

        var deliveryMethod = await unit.Repository<DeliveryMethod>().GetByIdAsync(deliveryMethodId);
        if (deliveryMethod == null) return null;

        var order = new Order
        {
            OrderItems = items,
            DeliveryMethod = deliveryMethod,
            ShippingAddress = shippingAddress,
            Subtotal = items.Sum(x => x.Price * x.Quantity),
            Discount = discount,
            PaymentSummary = paymentSummary,
            PaymentIntentId = cart.PaymentIntentId,
            BuyerEmail = buyerEmail
        };

        unit.Repository<Order>().Add(order);

        if (await unit.Complete())
        {
            return order;
        }

        return null;
    }

    public async Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string email)
    {
        var spec = new OrderSpecification(email);
        return await unit.Repository<Order>().ListAsync(spec);
    }

    public async Task<Order?> GetOrderByIdAsync(string email, int id)
    {
        var spec = new OrderSpecification(email, id);
        return await unit.Repository<Order>().GetEntityWithSpec(spec);
    }
}

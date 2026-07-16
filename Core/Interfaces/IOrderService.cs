using System;
using Core.Entities;
using Core.Entities.OrderAggregate;

namespace Core.Interfaces;

public interface IOrderService
{
    Task<Order?> CreateOrderAsync(string buyerEmail, int deliveryMethodId, string cartId,
        ShippingAddress shippingAddress, PaymentSummary paymentSummary, decimal discount);
    Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string email);
    Task<Order?> GetOrderByIdAsync(string email, int id);
    Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync();
}

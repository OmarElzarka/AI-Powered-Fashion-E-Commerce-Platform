using System;
using Core.DTOs;
using Core.Extensions;
using API.Extensions;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class OrdersController(IOrderService orderService) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto orderDto)
    {
        var email = User.GetEmail();

        var order = await orderService.CreateOrderAsync(
            email,
            orderDto.DeliveryMethodId,
            orderDto.CartId,
            orderDto.ShippingAddress,
            orderDto.PaymentSummary,
            orderDto.Discount
        );

        if (order == null) return BadRequest("Problem creating order");

        return order.ToDto();
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrdersForUser()
    {
        var email = User.GetEmail();
        var orders = await orderService.GetOrdersForUserAsync(email);
        var ordersToReturn = orders.Select(o => o.ToDto()).ToList();
        return Ok(ordersToReturn);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        var email = User.GetEmail();
        var order = await orderService.GetOrderByIdAsync(email, id);
        if (order == null) return NotFound();
        return order.ToDto();
    }
}

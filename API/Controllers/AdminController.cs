using System;
using Core.DTOs;
using Core.Extensions;
using Core.RequestHelpers;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(IAdminService adminService) : BaseApiController
{
    [HttpGet("dashboard-stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
    {
        return await adminService.GetDashboardStatsAsync();
    }

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers()
    {
        var users = await adminService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("orders")]
    public async Task<ActionResult<Pagination<OrderDto>>> GetOrders([FromQuery] OrderSpecParams specParams)
    {
        var pagination = await adminService.GetOrdersAsync(specParams);
        return Ok(pagination);
    }

    [HttpGet("orders/{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        var order = await adminService.GetOrderByIdAsync(id);
        if (order == null) return BadRequest("No order with that Id");
        return Ok(order);
    }

    [HttpPost("orders/refund/{id:int}")]
    public async Task<ActionResult<OrderDto>> RefundOrder(int id)
    {
        var order = await adminService.RefundOrderAsync(id);
        
        if (order == null) return BadRequest("Problem refunding order or order not found");
        
        return Ok(order);
    }

    [HttpDelete("users/{id}")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        var success = await adminService.DeleteUserAsync(id);
        if (success) return Ok();

        return BadRequest("Failed to delete user or user not found");
    }

    [HttpPost("users/{id}/roles")]
    public async Task<ActionResult> UpdateUserRole(string id, [FromQuery] string role, [FromQuery] bool assign)
    {
        var success = await adminService.UpdateUserRoleAsync(id, role, assign);
        if (success) return Ok();

        return BadRequest("Failed to update user role or user not found");
    }
}

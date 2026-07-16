using Core.DTOs;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Extensions;
using Core.Interfaces;
using Core.RequestHelpers;
using Core.Specifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services;

public class AdminService(IUnitOfWork unit, IPaymentService paymentService, UserManager<AppUser> userManager) : IAdminService
{
    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var totalProducts = await unit.Repository<Product>().CountAsync(new ProductSpecification(new ProductSpecParams()));
        var totalOrders = await unit.Repository<Order>().CountAsync(new OrderSpecification(new OrderSpecParams()));
        var totalUsers = await userManager.Users.CountAsync();

        return new DashboardStatsDto
        {
            TotalProducts = totalProducts,
            TotalOrders = totalOrders,
            TotalUsers = totalUsers
        };
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync()
    {
        var users = await userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles
            });
        }

        return userDtos;
    }

    public async Task<Pagination<OrderDto>> GetOrdersAsync(OrderSpecParams specParams)
    {
        var spec = new OrderSpecification(specParams);
        var repo = unit.Repository<Order>();

        var items = await repo.ListAsync(spec);
        var count = await repo.CountAsync(spec);

        var dtoItems = items.Select(o => o.ToDto()).ToList();

        return new Pagination<OrderDto>(specParams.PageIndex, specParams.PageSize, count, dtoItems);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var spec = new OrderSpecification(id);
        var order = await unit.Repository<Order>().GetEntityWithSpec(spec);
        
        return order?.ToDto();
    }

    public async Task<OrderDto?> RefundOrderAsync(int id)
    {
        var spec = new OrderSpecification(id);
        var order = await unit.Repository<Order>().GetEntityWithSpec(spec);

        if (order == null) return null;
        if (order.Status == OrderStatus.Pending) return null; // Or throw exception

        var result = await paymentService.RefundPayment(order.PaymentIntentId);

        if (result == "succeeded")
        {
            order.Status = OrderStatus.Refunded;
            await unit.Complete();
            return order.ToDto();
        }

        return null;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return false;

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> UpdateUserRoleAsync(string id, string role, bool assign)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return false;

        IdentityResult result;
        if (assign)
        {
            result = await userManager.AddToRoleAsync(user, role);
        }
        else
        {
            result = await userManager.RemoveFromRoleAsync(user, role);
        }

        return result.Succeeded;
    }
}

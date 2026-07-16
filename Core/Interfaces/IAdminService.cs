using Core.DTOs;
using Core.RequestHelpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Specifications;

namespace Core.Interfaces;

public interface IAdminService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<IReadOnlyList<UserDto>> GetUsersAsync();
    Task<Pagination<OrderDto>> GetOrdersAsync(OrderSpecParams specParams);
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto?> RefundOrderAsync(int id);
    Task<bool> DeleteUserAsync(string id);
    Task<bool> UpdateUserRoleAsync(string id, string role, bool assign);
}

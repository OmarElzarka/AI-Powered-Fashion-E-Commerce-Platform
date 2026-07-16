using Core.DTOs;

namespace Core.Interfaces;

public interface INotificationService
{
    Task OrderCompleteNotificationAsync(string email, OrderDto order);
}

using Core.DTOs;
using Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR;

public class NotificationService(IHubContext<NotificationHub> hubContext) : INotificationService
{
    public async Task OrderCompleteNotificationAsync(string email, OrderDto order)
    {
        var connectionId = NotificationHub.GetConnectionIdByEmail(email);

        if (!string.IsNullOrEmpty(connectionId))
        {
            await hubContext.Clients.Client(connectionId).SendAsync("OrderCompleteNotification", order);
        }
    }
}

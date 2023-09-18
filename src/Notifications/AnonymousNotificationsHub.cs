using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using Bit.Core.Models;

namespace Bit.Notifications;

[AllowAnonymous]
public class AnonymousNotificationsHub : Microsoft.AspNetCore.SignalR.Hub, INotificationHub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine("Debug::AnonymousNotificationsHub OnConnectedAsync ...... "); 
        var httpContext = Context.GetHttpContext();
        var token = httpContext.Request.Query["Token"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(token))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, token);
            Console.WriteLine("Debug::AnonymousNotificationsHub adding connectionid ... " + Context.ConnectionId + " with token " + token);
        }
        await base.OnConnectedAsync();
    }
}

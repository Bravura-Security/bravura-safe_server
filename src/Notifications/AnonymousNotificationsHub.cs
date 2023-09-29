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
            Console.WriteLine("Debug::AnonymousNotificationsHub adding connectionid ... {0} with token {1}  :: {2}", Context.ConnectionId, token, DateTime.UtcNow);
        }
        await base.OnConnectedAsync();

        if (!string.IsNullOrWhiteSpace(token))
            HubHelpers._anonHubConnectionManager.AddConnection(Context.ConnectionId, token);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        await base.OnDisconnectedAsync(exception);

        HubHelpers._anonHubConnectionManager.RemoveConnection(Context.ConnectionId);
        _ = HubHelpers._hubConnectionManager.DeleteExpiredRequests("anon_", -1);
    }
}

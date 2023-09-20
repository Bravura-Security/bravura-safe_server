using System.Text.Json;
using Bit.Core.Enums;
using Bit.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace Bit.Notifications;

public static class HubHelpers
{
    private static JsonSerializerOptions _deserializerOptions =
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    private static System.Collections.Concurrent.ConcurrentStack<PushNotificationData<AuthRequestPushNotification>> _notificationStack =
            new System.Collections.Concurrent.ConcurrentStack<PushNotificationData<AuthRequestPushNotification>>();

    public static async Task SendNotificationToHubAsync(
        string notificationJson,
        IHubContext<NotificationsHub> hubContext,
        IHubContext<AnonymousNotificationsHub> anonymousHubContext,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Console.WriteLine("\nDebug::HubHelpers:: Received a notification inside SendNotificationToHubAsync .... {0} \n", notificationJson );
        var notification = JsonSerializer.Deserialize<PushNotificationData<object>>(notificationJson, _deserializerOptions);
        switch (notification.Type)
        {
            case PushType.SyncCipherUpdate:
            case PushType.SyncCipherCreate:
            case PushType.SyncCipherDelete:
            case PushType.SyncLoginDelete:
                var cipherNotification =
                    JsonSerializer.Deserialize<PushNotificationData<SyncCipherPushNotification>>(
                        notificationJson, _deserializerOptions);
                if (cipherNotification.Payload.UserId.HasValue)
                {
                    await hubContext.Clients.User(cipherNotification.Payload.UserId.ToString())
                        .SendAsync("ReceiveMessage", cipherNotification, cancellationToken);
                }
                else if (cipherNotification.Payload.OrganizationId.HasValue)
                {
                    await hubContext.Clients.Group(
                        $"Organization_{cipherNotification.Payload.OrganizationId}")
                        .SendAsync("ReceiveMessage", cipherNotification, cancellationToken);
                }
                break;
            case PushType.SyncFolderUpdate:
            case PushType.SyncFolderCreate:
            case PushType.SyncFolderDelete:
                var folderNotification =
                    JsonSerializer.Deserialize<PushNotificationData<SyncFolderPushNotification>>(
                        notificationJson, _deserializerOptions);
                await hubContext.Clients.User(folderNotification.Payload.UserId.ToString())
                        .SendAsync("ReceiveMessage", folderNotification, cancellationToken);
                break;
            case PushType.SyncCiphers:
            case PushType.SyncVault:
            case PushType.SyncOrgKeys:
            case PushType.SyncSettings:
            case PushType.LogOut:
                var userNotification =
                    JsonSerializer.Deserialize<PushNotificationData<UserPushNotification>>(
                        notificationJson, _deserializerOptions);
                await hubContext.Clients.User(userNotification.Payload.UserId.ToString())
                        .SendAsync("ReceiveMessage", userNotification, cancellationToken);
                break;
            case PushType.SyncSendCreate:
            case PushType.SyncSendUpdate:
            case PushType.SyncSendDelete:
                var sendNotification =
                    JsonSerializer.Deserialize<PushNotificationData<SyncSendPushNotification>>(
                            notificationJson, _deserializerOptions);
                await hubContext.Clients.User(sendNotification.Payload.UserId.ToString())
                    .SendAsync("ReceiveMessage", sendNotification, cancellationToken);
                break;
            case PushType.AuthRequestResponse:
            try 
            {
                Console.WriteLine("\nDebug::HubHelpers:: Sending AuthRequestResponse\n");

                var authRequestResponseNotification =
                    JsonSerializer.Deserialize<PushNotificationData<AuthRequestPushNotification>>(
                            notificationJson, _deserializerOptions);

                    // Notice the typo AuthRequestResponseRecieved do not change it, must match typo in client side
                    await anonymousHubContext.Clients.Group(authRequestResponseNotification.Payload.Id.ToString())
                        .SendAsync("AuthRequestResponseRecieved", authRequestResponseNotification, cancellationToken);

                    _notificationStack.Push(authRequestResponseNotification);

                    Console.WriteLine("\nDebug::HubHelpers:: Just sent to websocket AuthRequestResponseRecieved with payload ID == " + authRequestResponseNotification.Payload.Id.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nError::HubHelpers:: Exception sending authRequestResponseNotification \n" + ex.Message);
            }
                break;
            case PushType.AuthRequest:
            Console.WriteLine("\nDebug::HubHelpers:: Sending AuthRequest\n");
                var authRequestNotification =
                    JsonSerializer.Deserialize<PushNotificationData<AuthRequestPushNotification>>(
                            notificationJson, _deserializerOptions);
                await hubContext.Clients.User(authRequestNotification.Payload.UserId.ToString())
                    .SendAsync("ReceiveMessage", authRequestNotification, cancellationToken);
                break;
            default:
                break;
        }
    }

    public static async Task DoResend(IHubContext<AnonymousNotificationsHub> anonymousHubContext, CancellationToken cancellationToken)
    {
        //Console.WriteLine("Entering DoResend");
        while (_notificationStack.Count > 0)
        {
            PushNotificationData<AuthRequestPushNotification> authRequestResponseNotification = null;
            if (_notificationStack.TryPop(out authRequestResponseNotification))
            {
                string groupId = authRequestResponseNotification.Payload.Id.ToString();
                try
                {
                    // Attempt to send the message
                    // Notice the typo AuthRequestResponseRecieved do not change it, must match typo in client side
                    await anonymousHubContext.Clients.Group(groupId)
                        .SendAsync("AuthRequestResponseRecieved", authRequestResponseNotification, cancellationToken);

                    Console.WriteLine("Debug::: Executed HubHelpers::DoResend to AnonymousNotificationsHub groupId {0} ", groupId);
                }
                catch (Exception)
                {
                    // Group does not exist, handle the case accordingly
                    // You can log a message or take some other action here
                    Console.WriteLine("Debug::: HubHelpers::DoResend to AnonymousNotificationsHub groupId {0} does not exist, most likely connection already closed. ", groupId);
                }
            }
        }

        //Console.WriteLine("Exiting DoResend");
    }
}

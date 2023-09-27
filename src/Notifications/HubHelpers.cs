using System.Text.Json;
using Bit.Core.Enums;
using Bit.Core.Models;
using Bit.Core.Settings;
using LinqToDB;
using Microsoft.AspNetCore.SignalR;

namespace Bit.Notifications;

public static class HubHelpers
{
    private static JsonSerializerOptions _deserializerOptions =
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    public static HubConnectionManager _anonHubConnectionManager = new HubConnectionManager();
    public static HubConnectionManager _hubConnectionManager = new HubConnectionManager();

    public static void InitHubConnectionManager(GlobalSettings globalSettings)
    {
        _anonHubConnectionManager.GlobalSettings = globalSettings;
        _hubConnectionManager.GlobalSettings = globalSettings;
    }

    public static async Task SendNotificationToHubAsync(
        string notificationJson,
        IHubContext<NotificationsHub> hubContext,
        IHubContext<AnonymousNotificationsHub> anonymousHubContext,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Console.WriteLine("\nDebug::HubHelpers:: Received a notification inside SendNotificationToHubAsync .... {0} \n", notificationJson);
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

                    // Notice the typo AuthRequestResponseRecieved, do not change it, must match typo in client side
                    string payloadID = authRequestResponseNotification.Payload.Id.ToString();

                 
                    await anonymousHubContext.Clients.Group(payloadID)
                        .SendAsync("AuthRequestResponseRecieved", authRequestResponseNotification, cancellationToken);

                    Console.WriteLine("\nDebug::HubHelpers:: Just sent to websocket AuthRequestResponseRecieved with payload ID == " + payloadID);
                    

                    var l_token = _anonHubConnectionManager.FindKeyByValue(payloadID);
                    if (string.IsNullOrEmpty(l_token))
                    {
                        //wrong node processed the push so need to send for retry
                        //only dumping to file since on wrong node
                        await _anonHubConnectionManager.DumpToFile("", payloadID, notificationJson);
                        Console.WriteLine("\nDebug::HubHelpers:: incorrect node, adding authRequestResponseNotification to retry on correct node ... " + payloadID);
                    }
                    else
                    {
                        //for retry mechanism, might no longer be needed
                        //_anonConnectionManager._notificationStack.Push(authRequestResponseNotification);

                        // remove the connection tracking since I am the servicing node and no need
                        // to do anything for myself in DoResend()
                        HubHelpers._anonHubConnectionManager.RemoveConnectionByValue(l_token);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\nError::HubHelpers:: Exception sending authRequestResponseNotification \n" + ex.Message);
                }
                break;
            case PushType.AuthRequest:
                {
                    Console.WriteLine("\nDebug::HubHelpers:: Sending AuthRequest\n");
                    var authRequestNotification =
                        JsonSerializer.Deserialize<PushNotificationData<AuthRequestPushNotification>>(
                                notificationJson, _deserializerOptions);

                    await hubContext.Clients.User(authRequestNotification.Payload.UserId.ToString())
                        .SendAsync("ReceiveMessage", authRequestNotification, cancellationToken);

                    bool bAlwaysCreate = true;
                    if (bAlwaysCreate)
                    {
                        //always create in case other nodes are procesing websockets for this user
                        string strFileName = authRequestNotification.Payload.UserId.ToString();
                        strFileName = FileNameGenerator.SanitizeFileName(strFileName);
                        await _hubConnectionManager.DumpToFile("authreq_", strFileName, notificationJson);
                    }


                    var tmpConnID = _hubConnectionManager.FindKeyByValue(authRequestNotification.Payload.UserId.ToString());
                    //if (string.IsNullOrEmpty(tmpConnID) == false)
                    //    _hubConnectionManager.RemoveConnection(tmpConnID);

                    if (string.IsNullOrEmpty(tmpConnID))
                    {
                        //wrong node processed the push so need to send for retry
                        //only dumping to file since on wrong node
                        Console.WriteLine("\nDebug::HubHelpers:: incorrect node, adding PushType.AuthRequest to retry on correct node ... " + authRequestNotification.Payload.UserId.ToString());
                    }
                    else
                    {
                        //do not remove the connection since what if another node updates the same file and servicing node needs to read it.
                        //HubHelpers._hubConnectionManager.RemoveConnectionByValue(authRequestNotification.Payload.UserId.ToString());

                        // now could force read it myself since don't want to reprocess it
                        // but DumpToFile also sets that I just read it since I did after all just write it.
                        //await _hubConnectionManager.ReadFromFile(strFileName, DateTime.MinValue, HubConnectionManagerFileAction.DoNothing);
                    }
                }
                break;
            default:
                break;
        }
    }

    public static async Task DoAnonHubResend(IHubContext<AnonymousNotificationsHub> anonymousHubContext, CancellationToken cancellationToken)
    {
        // here need to read the notifications dumped to file that I am servicing. file names will be something like <folder>/notifications/<token>.json
        // Retrieve the connection IDs for all clients
        // remember connectionmanager only contains connections for my node, so iterate over those.

        // Send a message to each client individually
        foreach (var connectionId in HubHelpers._anonHubConnectionManager.GetAllKeys())
        {
            // now retrieve value for each key and read all files from that subfolder
            string token = HubHelpers._anonHubConnectionManager.FindValueByKey(connectionId);
            if (string.IsNullOrEmpty(token))
                continue;

            string strJson = await _anonHubConnectionManager.ReadFromFile("", token, DateTime.MinValue);//can't have more than one for anon connection
            if (string.IsNullOrEmpty(strJson))
                continue;

            var authRequestResponseNotification =
                    JsonSerializer.Deserialize<PushNotificationData<AuthRequestPushNotification>>(
                            strJson, _deserializerOptions);

            // just before sending check again if still servicing this connection
            token = HubHelpers._anonHubConnectionManager.FindValueByKey(connectionId);
            if (string.IsNullOrEmpty(token))
                continue;

            // Notice the typo AuthRequestResponseRecieved do not change it, must match typo in client side
            await anonymousHubContext.Clients.Group(authRequestResponseNotification.Payload.Id.ToString())
                .SendAsync("AuthRequestResponseRecieved", authRequestResponseNotification, cancellationToken);

            Console.WriteLine("\nHubHelpers::DoAnonHubResend sent from correct node {0} \n", token);
        }
        //Console.WriteLine("Exiting DoResend");
    }
    public static async Task DoHubResend(IHubContext<NotificationsHub> hubContext, CancellationToken cancellationToken)
    {
        // Send a message to each client individually
        
        foreach (var connectionId in HubHelpers._hubConnectionManager.GetAllKeys())
        {
            // now retrieve value for each key and read all files from that subfolder
            string token = HubHelpers._hubConnectionManager.FindValueByKey(connectionId);
            if (string.IsNullOrEmpty(token))
                continue;

            var timeStamp = HubHelpers._hubConnectionManager.GetConnectionTimestamp(connectionId);
            // use this timestamp since only want to read file if it was created after connection was started
            // ie do not send old messages
            // in hubconnectionmanager tracking the last file with this name that I processed and do not reprocess.

            string strFileName = token;
            strFileName = FileNameGenerator.SanitizeFileName(strFileName);
            string strJson = await _hubConnectionManager.ReadFromFile("authreq_", strFileName, timeStamp, HubConnectionManagerFileAction.DoNothing);//can have more than one for that auth request so keep file around
            if (string.IsNullOrEmpty(strJson))
                continue;

            var authRequestNotification =
                        JsonSerializer.Deserialize<PushNotificationData<AuthRequestPushNotification>>(
                                strJson, _deserializerOptions);

            // just before sending check again if still servicing this connection
            token = HubHelpers._hubConnectionManager.FindValueByKey(connectionId);
            if (string.IsNullOrEmpty(token))
                continue;

            await hubContext.Clients.User(authRequestNotification.Payload.UserId.ToString())
                .SendAsync("ReceiveMessage", authRequestNotification, cancellationToken);
        }
    }
}

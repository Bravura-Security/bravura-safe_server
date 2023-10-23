using System.Text.Json;
using Bit.Core.Models;
using Bit.Core.Repositories;
using Bit.Core.Settings;
using Microsoft.AspNetCore.SignalR;

namespace Bit.Notifications;

public class HubConnectionManagerService : IHostedService, IDisposable
{

    private const int EXPIRY_CHECK_MINUTES = 60*4;

    private readonly ILogger _logger;
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly IHubContext<AnonymousNotificationsHub> _anonymousHubContext;
    private readonly GlobalSettings _globalSettings;
    private IHubConnectionRepository _hubConnRepo;

    private Task _executingTask;
    private CancellationTokenSource _cts;

    private JsonSerializerOptions _deserializerOptions =
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    public HubConnectionManagerService(
        ILogger<HubConnectionManagerService> logger,
        IHubContext<NotificationsHub> hubContext,
        IHubContext<AnonymousNotificationsHub> anonymousHubContext,
        IHubConnectionRepository hubConnRepo,
        GlobalSettings globalSettings)
    {
        _logger = logger;
        _hubContext = hubContext;
        _globalSettings = globalSettings;
        _anonymousHubContext = anonymousHubContext;
        _hubConnRepo = hubConnRepo;
        HubHelpers.InitHubConnectionManager(globalSettings, hubConnRepo);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_cts.Token);
        return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask == null)
        {
            return;
        }
        _logger.LogWarning("Stopping service.");
        _cts.Cancel();
        await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
    }

    public void Dispose()
    { }

    private void DoExpiryCheck(DateTime cutoffTime)
    {
        try
        {
            _hubConnRepo.DeleteConnections(cutoffTime);
        }
        catch (Exception)
        {

        }
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var l_lastTime = DateTime.UtcNow;
        var lastExpiryCheck = DateTime.MinValue;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // running this every 5 seconds in order to not hammer the DB
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                
                var tmpTime = DateTime.UtcNow;
                var resultList = await _hubConnRepo.GetTokensAsync(l_lastTime, false);
                if (resultList != null)
                {
                    if (resultList.Count > 0)
                    {
                        //Console.WriteLine(" We have some results ...... count {0} ", resultList.Count);
                        foreach (var item in resultList)
                        {
                            if ((item.MessageType == null) || (item.MessagePayload == null))
                                continue;

                            if (item.MessageType.CompareTo("authreq_") == 0)
                            {
                                HubConnectionManager l_hubMngr = HubHelpers._hubConnectionManager;
                                // Get the connection ID from my internal map and not DB
                                // this way know I am the servicing node
                                var connectionId = l_hubMngr.FindKeyByValue(item.Token.ToString());
                                if (string.IsNullOrEmpty(connectionId))
                                    continue;
                                
                                // use this timestamp since only want to read file if it was created after connection was started
                                // ie do not send old messages
                                // in hubconnectionmanager tracking the last file with this name that I processed and do not reprocess.

                                var timeStamp = l_hubMngr.GetConnectionTimestamp(connectionId);
                                if (item.RevisionDate < timeStamp)
                                    continue;

                                /* ***
                                if (connectionId.CompareTo(item?.ConnectionId) == 0)
                                {
                                    Console.WriteLine("****______***** I will be sending this authreq, found a connection ID ***______*****");
                                }
                                *** */

                            } else if (item.MessageType.CompareTo("anon_") == 0)
                            {
                                //l_hubMngr = HubHelpers._anonHubConnectionManager;
                            }
                            
                            string strJson = item.MessagePayload;
                            if (string.IsNullOrEmpty(strJson))
                                continue;

                            //now do right thing if anon or authreq
                            //based on that flag call correct context
                            if (item.MessageType.CompareTo("authreq_")==0)
                            {
                                var authRequestNotification =
                                JsonSerializer.Deserialize<PushNotificationData<AuthRequestPushNotification>>(
                                    strJson, _deserializerOptions);

                                await _hubContext.Clients.User(authRequestNotification.Payload.UserId.ToString())
                                   .SendAsync("ReceiveMessage", authRequestNotification, cancellationToken);

                                Console.WriteLine("\nHubConnectionManagerService ** _hubContext authreq_ ** sent from correct node {0} UTC time: {1}\n", item.Token, DateTime.UtcNow);
                            }
                            else if (item.MessageType.CompareTo("anon_") == 0)
                            {
                                var authRequestResponseNotification =
                                    JsonSerializer.Deserialize<PushNotificationData<AuthRequestPushNotification>>(
                                            strJson, _deserializerOptions);

                                // Notice the typo AuthRequestResponseRecieved do not change it, must match typo in client side
                                await _anonymousHubContext.Clients.Group(authRequestResponseNotification.Payload.Id.ToString())
                                    .SendAsync("AuthRequestResponseRecieved", authRequestResponseNotification, cancellationToken);

                                Console.WriteLine("\nHubConnectionManagerService -- _anonymousHubContext -- sent from this node {0} UTC time: {1}\n", item.Token, DateTime.UtcNow);
                            }
                        }//end foreach
                    }
                }//if (resultList != null)
                l_lastTime = tmpTime;


                // now maybe check and delete expired requests
                // once every EXPIRY_CHECK_MINUTES minutes should be good enough
                DateTime currentUtcTime = DateTime.UtcNow;

                // Calculate the time elapsed since the specific UTC time
                TimeSpan elapsed = currentUtcTime - lastExpiryCheck;

                // Check if 30 minutes have elapsed
                if (elapsed.TotalMinutes >= EXPIRY_CHECK_MINUTES)
                {
                    //Console.WriteLine("{0} minutes have elapsed since the last expiry check.", EXPIRY_CHECK_MINUTES);
                    DoExpiryCheck(DateTime.Now.AddMinutes(-EXPIRY_CHECK_MINUTES).ToUniversalTime());
                    lastExpiryCheck = currentUtcTime;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        _logger.LogWarning("Done processing.");
    }
}

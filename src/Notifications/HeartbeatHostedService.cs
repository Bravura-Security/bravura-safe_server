using Bit.Core.Settings;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace Bit.Notifications;

public class HeartbeatHostedService : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly IHubContext<AnonymousNotificationsHub> _anonymousHubContext;
    private readonly GlobalSettings _globalSettings;

    private Task _executingTask;
    private CancellationTokenSource _cts;

    public HeartbeatHostedService(
        ILogger<HeartbeatHostedService> logger,
        IHubContext<NotificationsHub> hubContext,
        IHubContext<AnonymousNotificationsHub> anonymousHubContext,
        GlobalSettings globalSettings)
    {
        _logger = logger;
        _hubContext = hubContext;
        _anonymousHubContext = anonymousHubContext;
        _globalSettings = globalSettings;
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

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // max 25 second total delay and added sending heartbeat to anonymous hub as well
            await Task.Delay(5000, cancellationToken);
            await _hubContext.Clients.All.SendAsync("Heartbeat");
            if (_anonymousHubContext!=null)
            {
                await _anonymousHubContext.Clients.All.SendAsync("Heartbeat");
            }
            _logger.LogInformation("Sent heartbeat.");
            await Task.Delay(15000, cancellationToken);

            if (_anonymousHubContext!=null)
            {
                await HubHelpers.DoResend(_anonymousHubContext, cancellationToken);
            }
            await Task.Delay(5000, cancellationToken);
        }
        _logger.LogWarning("Done with heartbeat.");
    }
}

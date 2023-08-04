using Bit.Core.Jobs;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Microsoft.Extensions.Options;
using Quartz;

namespace Bit.Admin.Jobs;

public class DeleteUnusedDevicesJob : BaseJob
{
    private readonly IDeviceRepository _deviceRepository;
    private IDeviceService _deviceService;
    private readonly AdminSettings _adminSettings;
    public DeleteUnusedDevicesJob(IDeviceRepository deviceRepository, IDeviceService deviceService,
        IOptions<AdminSettings> adminSettings, ILogger logger) : base(logger)
    {
        _deviceRepository = deviceRepository;
        _deviceService = deviceService;
        _adminSettings = adminSettings?.Value;
    }

    protected async override Task ExecuteJobAsync(IJobExecutionContext context)
    {

        var olderThan = DateTime.UtcNow.AddDays(-90);
        var devices = await _deviceRepository.GetUnusedDevicesAsync(olderThan);
        foreach (var device in devices)
        {
            await _deviceService.DeleteAsync(device);
        }
    }
}

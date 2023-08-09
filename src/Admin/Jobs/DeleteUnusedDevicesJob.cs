using Bit.Core.Jobs;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Quartz;

namespace Bit.Admin.Jobs;

public class DeleteUnusedDevicesJob : BaseJob
{
    private readonly IDeviceRepository _deviceRepository;
    private IDeviceService _deviceService;
    public DeleteUnusedDevicesJob(IDeviceRepository deviceRepository, IDeviceService deviceService,
        ILogger<DeleteUnusedDevicesJob> logger) : base(logger)
    {
        _deviceRepository = deviceRepository;
        _deviceService = deviceService;
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

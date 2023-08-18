using Bit.Core.Entities;

namespace Bit.Core.Repositories;
public interface IAmazonSNSDeviceRepository : IRepository<AmazonSNSDevice, long>
{
    public Task DeleteAsync(Guid deviceID);

    public Task<AmazonSNSDevice> GetByDeviceIDAsync(Guid deviceID);
}

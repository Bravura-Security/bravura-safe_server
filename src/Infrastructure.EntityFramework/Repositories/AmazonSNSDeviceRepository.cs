using AutoMapper;
using Bit.Core.Repositories;
using Bit.Infrastructure.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Infrastructure.EntityFramework.Repositories;
public class AmazonSNSDeviceRepository : Repository<Core.Entities.AmazonSNSDevice, AmazonSNSDevice, long>, IAmazonSNSDeviceRepository
{
    public AmazonSNSDeviceRepository(IServiceScopeFactory serviceScopeFactory, IMapper mapper)
    : base(serviceScopeFactory, mapper, (DatabaseContext context) => context.AmazonSNSDevices)
    { }
    public async Task DeleteAsync(Guid deviceID)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var entity = await GetDbSet(dbContext).SingleOrDefaultAsync(su => su.DeviceID == deviceID);
            dbContext.Entry(entity).State = EntityState.Deleted;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<Core.Entities.AmazonSNSDevice> GetByDeviceIDAsync(Guid deviceID)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var query = dbContext.AmazonSNSDevices.Where(d => d.DeviceID == deviceID);
            var amazonsnsdevice = await query.FirstOrDefaultAsync();
            return Mapper.Map<Core.Entities.AmazonSNSDevice>(amazonsnsdevice);
        }
    }
}

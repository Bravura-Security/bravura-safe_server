using AutoMapper;
using Bit.Core.Repositories;
using Bit.Infrastructure.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Infrastructure.EntityFramework.Repositories;
public class HubConnectionRepository: Repository<Core.Entities.HubConnection, HubConnection, long>, IHubConnectionRepository
{
    public HubConnectionRepository(IServiceScopeFactory serviceScopeFactory, IMapper mapper)
    : base(serviceScopeFactory, mapper, (DatabaseContext context) => context.HubConnections)
    { }

    public async Task DeleteAsync(string connectionID)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var entity = await GetDbSet(dbContext).SingleOrDefaultAsync(su => su.ConnectionId == connectionID);
            dbContext.Entry(entity).State = EntityState.Deleted;
            await dbContext.SaveChangesAsync();
        }
    }

    async Task<ICollection<Core.Entities.HubConnection>> IHubConnectionRepository.GetTokensAsync(DateTime olderThan, bool GetEmptyPayload)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            IQueryable<HubConnection> query;
            if (GetEmptyPayload)
                query = dbContext.HubConnections.Where(d => d.RevisionDate < olderThan);
            else
                query = dbContext.HubConnections.Where(d => d.RevisionDate < olderThan && d.MessagePayload != null);

            var hubConnections = await query.ToListAsync();
            return Mapper.Map<List<Core.Entities.HubConnection>>(hubConnections);
        }
    }
    async Task<Core.Entities.HubConnection> IHubConnectionRepository.GetByTokenAsync(Guid token, DateTime iDateSince, string msgType)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var entity = await GetDbSet(dbContext).SingleOrDefaultAsync(su => su.Token == token);
            //dbContext.Entry(entity).State = EntityState.Deleted;
            //await dbContext.SaveChangesAsync();
            return entity;
        }
    }

    async Task IHubConnectionRepository.SaveNotificationPayload(Guid token, string msgType, string jsonPayload)
    {
        Console.WriteLine("**** this is very very very bad ****************************************");
        throw new NotImplementedException();
    }

    async Task<ICollection<Core.Entities.HubConnection>> IHubConnectionRepository.GetConnectionByTokenDateAsync(Guid token, DateTime newerThan)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var query = dbContext.HubConnections.Where(d => d.RevisionDate > newerThan && d.Token == token);
            var hubConnections = await query.ToListAsync();
            return Mapper.Map<List<Core.Entities.HubConnection>>(hubConnections);
        }
    }

    async Task IHubConnectionRepository.DeleteConnections(DateTime dateOlderThan)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var entity = dbContext.HubConnections.Where(d => d.RevisionDate < dateOlderThan);
            dbContext.HubConnections.RemoveRange(entity);
            await dbContext.SaveChangesAsync();
        }
    }
}

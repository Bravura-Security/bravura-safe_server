using Bit.Core.Entities;

namespace Bit.Core.Repositories;
public  interface IHubConnectionRepository : IRepository<HubConnection, long>
{
    public Task<HubConnection> GetByTokenAsync(Guid token, DateTime iDateTime, string msgType);
    public Task DeleteAsync(string connectionID);
    public Task<ICollection<HubConnection>> GetTokensAsync(DateTime newerThan, bool GetEmptyPayload);
    public Task<ICollection<HubConnection>> GetConnectionByTokenDateAsync(Guid token, DateTime newerThan);
    public Task SaveNotificationPayload(Guid token, string msgType, string jsonPayload);
    public Task DeleteConnections(DateTime dateOlderThan);
}

using System.Data;
using Bit.Core.Entities;
using Bit.Core.Repositories;
using Bit.Core.Settings;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Bit.Infrastructure.Dapper.Repositories;

public class HubConnectionRepository : Repository<HubConnection, long>, IHubConnectionRepository
{
    public HubConnectionRepository(GlobalSettings globalSettings)
        : this(globalSettings.SqlServer.ConnectionString, globalSettings.SqlServer.ReadOnlyConnectionString)
    { }

    public HubConnectionRepository(string connectionString, string readOnlyConnectionString)
        : base(connectionString, readOnlyConnectionString)
    { }

    public async Task<HubConnection> GetByTokenAsync(Guid token, DateTime iDateTime, string msgType)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.ExecuteAsync(
                $"[{Schema}].[HubConnection_GetByToken]",
                new { Token = token, MessageType = msgType, @Timestamp = iDateTime },
                commandType: CommandType.StoredProcedure);
        }

        return null;
    }

    public async Task DeleteAsync(string connectionId)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.ExecuteAsync(
                $"[{Schema}].[HubConnection_Delete]",
                new { ConnectionId = connectionId },
                commandType: CommandType.StoredProcedure);
        }
    }

    //Task<HubConnection> IHubConnectionRepository.GetByTokenAsync(Guid token, DateTime iDateSince) => throw new NotImplementedException();
    async Task<ICollection<HubConnection>> IHubConnectionRepository.GetTokensAsync(DateTime newerThan, bool GetEmptyPayload)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.QueryAsync<HubConnection>(
                $"[{Schema}].[{Table}_ReadByDateSince]",
                new { @NewerThan = newerThan, @ReturnEmptyPayload = GetEmptyPayload },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 43200);

            return results.ToList();
        }
    }

    async Task IHubConnectionRepository.SaveNotificationPayload(Guid token, string msgType, string jsonPayload)
    {
        //Console.WriteLine("**** Saving payload token {0} msgtype {1} *** payload: {2} ***", token.ToString(), msgType, jsonPayload);
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.QueryAsync<HubConnection>(
                $"[{Schema}].[{Table}_SaveNotificationPayload]",
                new { Token = token, MessageType = msgType, MessagePayload = jsonPayload},
                commandType: CommandType.StoredProcedure,
                commandTimeout: 43200);
        }
    }

    async Task<ICollection<HubConnection>> IHubConnectionRepository.GetConnectionByTokenDateAsync(Guid token, DateTime newerThan)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.QueryAsync<HubConnection>(
                $"[{Schema}].[{Table}_ReadByTokenDateSince]",
                new { @Token = token, @NewerThan = newerThan },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 43200);

            return results.ToList();
        }
    }

    async Task IHubConnectionRepository.DeleteConnections(DateTime dateOlderThan)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.QueryAsync<HubConnection>(
                $"[{Schema}].[{Table}_DeleteByDateSince]",
                new { @DateOlderThan = dateOlderThan },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 43200);
        }
    }
    
}

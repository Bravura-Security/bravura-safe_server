using System.Data;
using Bit.Core.Entities;
using Bit.Core.Repositories;
using Bit.Core.Settings;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Bit.Infrastructure.Dapper.Repositories;

public class AmazonSNSDeviceRepository : Repository<AmazonSNSDevice, long>, IAmazonSNSDeviceRepository
{
    public AmazonSNSDeviceRepository(GlobalSettings globalSettings)
        : this(globalSettings.SqlServer.ConnectionString, globalSettings.SqlServer.ReadOnlyConnectionString)
    { }

    public AmazonSNSDeviceRepository(string connectionString, string readOnlyConnectionString)
        : base(connectionString, readOnlyConnectionString)
    { }

    public async Task DeleteAsync(Guid deviceId)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.ExecuteAsync(
                $"[{Schema}].[AmazonSNSDevice_Delete]",
                new { DeviceID = deviceId },
                commandType: CommandType.StoredProcedure);
        }
    }

    public async Task<AmazonSNSDevice> GetByDeviceIDAsync(Guid deviceId)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.QueryAsync<AmazonSNSDevice>(
                $"[{Schema}].[AmazonSNSDevice_ReadByDeviceId]",
                new { DeviceID = deviceId },
                commandType: CommandType.StoredProcedure);

            return results.FirstOrDefault();
        }
    }
}

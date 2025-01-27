using Bit.Core.Settings;
using Bit.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Bit.Migrator;

public class SqlServerDbMigrator : IDbMigrator
{
    private readonly DbMigrator _migrator;

    private string GrafanaDBUser { get; set; }
    private string GrafanaDBUserPWD { get; set; }

    public SqlServerDbMigrator(GlobalSettings globalSettings, ILogger<DbMigrator> logger)
    {
        _migrator = new DbMigrator(globalSettings.SqlServer.ConnectionString, logger,
            globalSettings.SqlServer.SkipDatabasePreparation);

        GrafanaDBUser = globalSettings.Grafana.DBUser;
        GrafanaDBUserPWD = globalSettings.Grafana.DBUserPassword;
    }

    private bool CreateGrafanaUser(string userName, string userPWD, CancellationToken cancellationToken = default(CancellationToken))
    {
        return _migrator.CreateGrafanaUser(userName, userPWD, cancellationToken);
        /*
        if (string.IsNullOrWhiteSpace(userName))
        {
            Console.WriteLine("\n Failure in CreateGrafanaUser -- userName is empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(userPWD))
        {
            Console.WriteLine("\n Failure in CreateGrafanaUser -- userPWD is empty");
            return false;
        }

        // create grafana user
        using (var connection = new SqlConnection(_masterConnectionString))
        {
            var databaseName = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                databaseName = "vault";
            }
            var databaseNameQuoted = new SqlCommandBuilder().QuoteIdentifier(databaseName);
            var grafanaUser = userName;

            var cmdText= DBUSER_CREATE_LOGIN;
            if (bContainedDB)
                cmdText = DBUSER_CREATE_LOGIN_CONTAINED_WCHECK;
                
            cmdText = cmdText.Replace("%databaseNameQuoted%", databaseNameQuoted);
            cmdText = cmdText.Replace("%grafanaUser%", grafanaUser);
            cmdText = cmdText.Replace("%userPWD%", userPWD);

            var command = new SqlCommand(cmdText, connection);

            Console.WriteLine("\n Attempting CreateGrafanaUser: {0} .. is contained DB == {1} ", userName, bContainedDB);
            //Console.WriteLine(command.CommandText);

            command.Parameters.Add("@DatabaseName", SqlDbType.VarChar).Value = databaseNameQuoted;
            command.Connection.Open();
            command.ExecuteNonQuery();
        }

        cancellationToken.ThrowIfCancellationRequested();

        return true;
        */
    }

    public bool MigrateDatabase(bool enableLogging = true,
        CancellationToken cancellationToken = default)
    {
        return _migrator.MigrateMsSqlDatabaseWithRetries(enableLogging,
            cancellationToken: cancellationToken);
    }
}

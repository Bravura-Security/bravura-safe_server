using System.Data;
using System.Reflection;
using Bit.Core;
using DbUp;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Bit.Migrator;

public class DbMigrator
{
    private readonly string _connectionString;
    private readonly ILogger<DbMigrator> _logger;
    private readonly string _masterConnectionString;

    public string GrafanaDBUser { get; set; }
    public string GrafanaDBUserPWD { get; set; }

    public DbMigrator(string connectionString, ILogger<DbMigrator> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
        _masterConnectionString = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;
    }

    public bool MigrateMsSqlDatabaseWithRetries(bool enableLogging = true,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        var attempt = 1;

        while (attempt < 10)
        {
            try
            {
                var success = MigrateDatabase(enableLogging, cancellationToken);
                return success;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Server is in script upgrade mode"))
                {
                    attempt++;
                    _logger.LogInformation("Database is in script upgrade mode. " +
                        $"Trying again (attempt #{attempt})...");
                    Thread.Sleep(20000);
                }
                else
                {
                    throw;
                }
            }
        }
        return false;
    }

    private bool CreateGrafanaUser(string userName, string userPWD, CancellationToken cancellationToken = default(CancellationToken))
    {
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
            var command = new SqlCommand(
                "USE " + databaseNameQuoted + "\n" +
                //"GO\n"+
                "IF SUSER_ID (N'" + grafanaUser + "') IS NULL\n" +
                "BEGIN\n" +
                "CREATE LOGIN " + grafanaUser + " WITH PASSWORD = '" + userPWD + "';\n" +
                "CREATE USER " + grafanaUser + " FOR LOGIN " + grafanaUser  + " ;\n" +
                "GRANT select ON Schema:: [DBO] TO " + grafanaUser + " ;\n" +
                "END \n" +
                "", connection);

            command.Parameters.Add("@DatabaseName", SqlDbType.VarChar).Value = databaseNameQuoted;
            command.Connection.Open();
            command.ExecuteNonQuery();
        }

        cancellationToken.ThrowIfCancellationRequested();

        return true;
    }

    public bool MigrateDatabase(bool enableLogging = true,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        if (_logger != null)
        {
            _logger.LogInformation(Constants.BypassFiltersEventId, "Migrating database.");
        }

        using (var connection = new SqlConnection(_masterConnectionString))
        {
            var databaseName = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                databaseName = "vault";
            }

            var databaseNameQuoted = new SqlCommandBuilder().QuoteIdentifier(databaseName);
            var command = new SqlCommand(
                "IF ((SELECT COUNT(1) FROM sys.databases WHERE [name] = @DatabaseName) = 0) " +
                "CREATE DATABASE " + databaseNameQuoted + ";", connection);
            command.Parameters.Add("@DatabaseName", SqlDbType.VarChar).Value = databaseName;
            command.Connection.Open();
            command.ExecuteNonQuery();

            command.CommandText = "IF ((SELECT DATABASEPROPERTYEX([name], 'IsAutoClose') " +
                "FROM sys.databases WHERE [name] = @DatabaseName) = 1) " +
                "ALTER DATABASE " + databaseNameQuoted + " SET AUTO_CLOSE OFF;";
            command.ExecuteNonQuery();
        }

        cancellationToken.ThrowIfCancellationRequested();
        using (var connection = new SqlConnection(_connectionString))
        {
            // Rename old migration scripts to new namespace.
            var command = new SqlCommand(
                "IF OBJECT_ID('Migration','U') IS NOT NULL " +
                "UPDATE [dbo].[Migration] SET " +
                "[ScriptName] = REPLACE([ScriptName], 'Bit.Setup.', 'Bit.Migrator.');", connection);
            command.Connection.Open();
            command.ExecuteNonQuery();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var builder = DeployChanges.To
            .SqlDatabase(_connectionString)
            .JournalToSqlTable("dbo", "Migration")
            .WithScriptsAndCodeEmbeddedInAssembly(Assembly.GetExecutingAssembly(),
                s => s.Contains($".DbScripts.") && !s.Contains(".Archive."))
            .WithTransaction()
            .WithExecutionTimeout(new TimeSpan(0, 5, 0));

        if (enableLogging)
        {
            if (_logger != null)
            {
                builder.LogTo(new DbUpLogger(_logger));
            }
            else
            {
                builder.LogToConsole();
            }
        }

        var upgrader = builder.Build();
        var result = upgrader.PerformUpgrade();

        if (_logger != null)
        {
            if (result.Successful)
            {
                _logger.LogInformation(Constants.BypassFiltersEventId, "Migration successful.");
            }
            else
            {
                _logger.LogError(Constants.BypassFiltersEventId, result.Error, "Migration failed.");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            CreateGrafanaUser(GrafanaDBUser, GrafanaDBUserPWD, cancellationToken);
        }
        catch (Exception)
        {
            // eat the exception since don't necessarile care if the grafana user is not created.
        }

        return result.Successful;
    }
}

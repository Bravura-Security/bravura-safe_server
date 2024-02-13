using System.Data;
using System.Reflection;
using Bit.Core;
using Bit.Core.Settings;
using Bit.Core.Utilities;
using DbUp;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Bit.Migrator;

public class SqlServerDbMigrator : IDbMigrator
{
    private readonly string _connectionString;
    private readonly ILogger<SqlServerDbMigrator> _logger;
    private readonly string _masterConnectionString;

    private string GrafanaDBUser { get; set; }
    private string GrafanaDBUserPWD { get; set; }

    private bool bContainedDB = false;

    public SqlServerDbMigrator(GlobalSettings globalSettings, ILogger<SqlServerDbMigrator> logger)
    {
        _connectionString = globalSettings.SqlServer.ConnectionString;
        _logger = logger;
        _masterConnectionString = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;

        GrafanaDBUser = globalSettings.Grafana.DBUser;
        GrafanaDBUserPWD = globalSettings.Grafana.DBUserPassword;
    }

    private string DBUSER_CREATE_LOGIN = @"USE %databaseNameQuoted%
    IF SUSER_ID (N'%grafanaUser%') IS NULL
    BEGIN
            CREATE LOGIN %grafanaUser% WITH PASSWORD = N'%userPWD%' , DEFAULT_DATABASE = %databaseNameQuoted%, CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF ;
            CREATE USER %grafanaUser% FOR LOGIN %grafanaUser% ;
            GRANT select ON Schema:: [DBO] TO %grafanaUser% ;
            USE [master];
            DENY VIEW ANY DATABASE TO [%grafanaUser%];
    END ";

    private string DBUSER_CREATE_LOGIN_CONTAINED_WCHECK =@"
    USE %databaseNameQuoted%;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_principals
        WHERE name = '%grafanaUser%' AND type_desc = 'SQL_USER'
    )
    BEGIN
        CREATE USER %grafanaUser% WITH PASSWORD = N'%userPWD%';
        -- Grant necessary permissions here
        GRANT select ON Schema:: [DBO] TO %grafanaUser% ;
    END;
    ";

    private bool CreateGrafanaUser(string userName, string userPWD, CancellationToken cancellationToken = default(CancellationToken))
    {

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
    }

    public bool MigrateDatabase(bool enableLogging = true,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        if (enableLogging && _logger != null)
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
            
            SqlCommand command = null;
            try
            {
                command = new SqlCommand(
                "IF ((SELECT COUNT(1) FROM sys.databases WHERE [name] = @DatabaseName) = 0) " +
                "CREATE DATABASE " + databaseNameQuoted + "\n CONTAINMENT = PARTIAL;", connection);

                command.Parameters.Add("@DatabaseName", SqlDbType.VarChar).Value = databaseName;
                command.Connection.Open();
                command.ExecuteNonQuery();
                bContainedDB = true;

                Console.WriteLine("*** Product DB created with CONTAINMENT = PARTIAL ");
            }
            catch (Exception)
            {
                // oh oh failed to create as contained DB
                // so try to create as regular
                command.Connection.Close(); // Close the connection otherwise will fail when try for non contained DB
                bContainedDB = false;
            }

            if (bContainedDB==false) //get here because failed to create as contained
            {
                command = new SqlCommand(
                "IF ((SELECT COUNT(1) FROM sys.databases WHERE [name] = @DatabaseName) = 0) " +
                "CREATE DATABASE " + databaseNameQuoted + ";", connection);

                command.Parameters.Add("@DatabaseName", SqlDbType.VarChar).Value = databaseName;
                command.Connection.Open();
                command.ExecuteNonQuery();

                Console.WriteLine("*** Product DB created without CONTAINMENT ");
            }                     

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
            .WithExecutionTimeout(TimeSpan.FromMinutes(5));

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

        if (enableLogging && _logger != null)
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
        catch (Exception ex)
        {
            // eat the exception since don't necessarily care if the grafana user is not created.
            Console.WriteLine("Failed creating user for Grafana: " + ex.Message);
        }

        return result.Successful;
    }
}

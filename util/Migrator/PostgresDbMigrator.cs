using Bit.Core;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Bit.Migrator;
public class PostgresDbMigrator
{
    private readonly string _connectionString;
    private readonly ILogger<DbMigrator> _logger;
    private readonly string _masterConnectionString;

    public PostgresDbMigrator(string connectionString, ILogger<DbMigrator> logger)
    {
        _connectionString = connectionString;
        _logger = logger;

        // Use the NpgsqlConnectionStringBuilder for better connection string management
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        // Clear the Database property
        builder.Remove("Database");
        
        _masterConnectionString = builder.ConnectionString; // this is used to connect to database server and not a specific DB
    }

    public bool MigrateDatabase(bool enableLogging = true,
        bool repeatable = false,
        string folderName = MigratorConstants.DefaultMigrationsFolderName,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        if (enableLogging && _logger != null)
        {
            _logger.LogInformation(Constants.BypassFiltersEventId, "Migrating database.");
        }

        try
        {
            // Try to connect to the database specified in the connection string
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                // SQL command to check if the database connection works
                string testQuery = "SELECT 1";
                using (var cmd = new NpgsqlCommand(testQuery, conn))
                {
                    cmd.ExecuteScalar();
                    Console.WriteLine($"Database '{conn.Database}' already exists and is accessible.");
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (NpgsqlException ex) when (ex.Message.Contains("does not exist"))
        {
            Console.WriteLine("Database does not exist, creating it...");
            try
            {
                using (var connection = new NpgsqlConnection(_masterConnectionString))
                {
                    connection.Open();
                    var databaseName = new NpgsqlConnectionStringBuilder(_connectionString).Database;
                    var dbUser = new NpgsqlConnectionStringBuilder(_connectionString).Username;
                    if (string.IsNullOrWhiteSpace(databaseName))
                    {
                        databaseName = "vault";
                    }

                    // Database does not exist, so create it
                    string createDbCommandTxt = $"CREATE DATABASE \"{databaseName}\"";

                    //var databaseNameQuoted = new NpgsqlCommandBuilder().QuoteIdentifier(databaseName);

                    using (var createDbCommand = new NpgsqlCommand(createDbCommandTxt, connection))
                    {
                        createDbCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception createEx)
            {
                Console.WriteLine($"Error creating database: {createEx.Message}");
                return false;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        
        return true;
    } //MigrateDatabase
}

using Microsoft.Data.SqlClient;
using Npgsql;

namespace Bit.Setup;

public class EnvironmentFileBuilder
{
    private readonly Context _context;

    private IDictionary<string, string> _globalValues;
    private IDictionary<string, string> _mssqlValues;
    private IDictionary<string, string> _globalOverrideValues;
    private IDictionary<string, string> _mssqlOverrideValues;
    private IDictionary<string, string> _keyConnectorOverrideValues;
    //private IDictionary<string, string> _awsSNSOverrideValues;

    private IDictionary<string, string> _grafanaOverrideValues;
    private IDictionary<string, string> _pgsqlValues;
    private IDictionary<string, string> _pgsqlOverrideValues;

    public EnvironmentFileBuilder(Context context)
    {
        _context = context;
        _globalValues = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["globalSettings__selfHosted"] = "true",
            ["globalSettings__baseServiceUri__vault"] = "http://localhost",
            //["globalSettings__pushRelayBaseUri"] = "https://push.safe.hitachi-id.net",
            ["globalSettings__pushRelayBaseUri"] = "REPLACE",
        };
        _mssqlValues = new Dictionary<string, string>
        {
            ["ACCEPT_EULA"] = "Y",
            ["MSSQL_PID"] = "Express",
            ["SA_PASSWORD"] = "SECRET",
        };

        _pgsqlValues = new Dictionary<string, string>
        {
            ["POSTGRES_PASSWORD"] = "SECRET",
        };

        _mssqlOverrideValues = new Dictionary<string, string>(); //init to empty
        _pgsqlOverrideValues = new Dictionary<string, string>(); //init to empty
    }

    public void BuildForInstaller()
    {
        Directory.CreateDirectory("/bitwarden/env/");
        Init();
        Build();
    }

    public void BuildForUpdater()
    {
        Init(false);
        LoadExistingValues(_globalOverrideValues, "/bitwarden/env/global.override.env");
        if (_mssqlOverrideValues?.Count > 0) LoadExistingValues(_mssqlOverrideValues, "/bitwarden/env/mssql.override.env");
        if (_pgsqlOverrideValues?.Count > 0) LoadExistingValues(_pgsqlOverrideValues, "/bitwarden/env/pgsql.override.env");
        LoadExistingValues(_keyConnectorOverrideValues, "/bitwarden/env/key-connector.override.env");

        if (_context.Config.PushNotifications &&
            _globalOverrideValues.ContainsKey("globalSettings__pushRelayBaseUri") &&
            _globalOverrideValues["globalSettings__pushRelayBaseUri"] == "REPLACE")
        {
            _globalOverrideValues.Remove("globalSettings__pushRelayBaseUri");
        }

        Build();
    }

    private void Init(bool forInstall = true)
    {
        var dbSource = "";
        var dbUser = "";
        var dbPassword = "";
        var dbCatalog = "";

        var dbGrafanaDBUser = "";
        var dbGrafanaDBUserPassword = "";
        var grafanaDefaultAdminPassword = "";
        var dbProvider = "";
        var dbPgSqlPort = "";

        if (forInstall)
        {
            // Get from input which DB provider will be using.
            dbProvider = Helpers.ReadInput("Enter your Database Provider.\n 1. Sql Server (Default) \n 2. PostgreSQL\n Default will use mssql (ie sqlserver). [sqlserver/postgres]");
            if (string.IsNullOrEmpty(dbProvider))
            {
                dbProvider = "sqlserver";
            }

            switch (dbProvider.ToLowerInvariant())
            {
                case "postgres":
                case "postgresql":
                case "2":
                    dbProvider = "postgres";
                    break;

                case "sqlserver":
                case "1":
                default:
                    dbProvider = "sqlserver";
                    break;
            }

            if (dbProvider.CompareTo("sqlserver") == 0)
            {
                dbSource = Helpers.ReadInput("Enter your Database Server name. Default will use a local mssql docker. [tcp:mssql,1433]");
                if (string.IsNullOrEmpty(dbSource))
                    Helpers.WriteLine(_context, "Default local docker will be used. The Database User will be sa.");
                else
                    dbUser = Helpers.ReadInput("Enter your Database User name [sa]");
                
                _context.Config.UseMssqlDocker = string.IsNullOrEmpty(dbSource) & string.IsNullOrEmpty(dbUser);
            }
            else if (dbProvider.CompareTo("postgres") == 0)
            {
                dbSource = Helpers.ReadInput("Enter your Postgres Database Server name. Default will use a local postgresql docker. [postgres]");
                if (string.IsNullOrEmpty(dbSource))
                {
                    Helpers.WriteLine(_context, "Default local docker will be used. The Database User will be postgres.");
                }
                else
                {
                    dbPgSqlPort = Helpers.ReadInput("Enter your Postgres DB Server port to use [5432]");
                    dbUser = Helpers.ReadInput("Enter your Database User name [postgres]");
                }

                if (string.IsNullOrEmpty(dbSource) && string.IsNullOrEmpty(dbUser))
                {
                    _context.Config.UsePostgresDocker = true;
                    dbUser = "postgres";
                    dbSource = "postgres";
                    var tmp = Helpers.ReadInput("Use a docker volume for Postgres DB data [Y/n]");
                    if (string.IsNullOrEmpty(tmp))
                        tmp = "y";
                    
                    if (tmp.ToLower().StartsWith("n"))
                        _context.Config.PostgresDataDockerVolume = false;
                    else
                        _context.Config.PostgresDataDockerVolume = true;
                }
            }
            else
            {
                Helpers.WriteLine(_context, "Fatal Error: Invalid database provider.");
            }

            dbPassword = _context.Stub ? "RANDOM_DATABASE_PASSWORD" : Helpers.ReadInput("Enter your Database User password [<randomly generated>]");
            dbCatalog = Helpers.ReadInput("Enter your Database name [vault]");
            if (string.IsNullOrEmpty(dbCatalog))
                dbCatalog = "vault";

            var customMailDev = Helpers.ReadInput("Use custom maildev container? [Y/n]");
            if (string.IsNullOrEmpty(customMailDev))
                customMailDev = "y";

            _context.Config.UseCustomMaildev = true;

            if ( customMailDev.ToLower().StartsWith("n"))
                _context.Config.UseCustomMaildev = false;

            var mailDevPass = Helpers.ReadInput("Require username and password (default admin/5*Hotel) for the maildev mail checking UI?\n Answering no implies no password protection on UI [y/N]");
            if (string.IsNullOrEmpty(mailDevPass))
                mailDevPass = "n";

            _context.Config.MaildevWebUserPassword = false;

            if (mailDevPass.ToLower().StartsWith("y"))
                _context.Config.MaildevWebUserPassword = true;

            // for now always include grafana so don't bother to ask.
            _context.Config.UseGrafanaDocker = true;

            dbGrafanaDBUser = (string.IsNullOrEmpty(dbCatalog) ? "vault" : dbCatalog) + "_grafana";
            dbGrafanaDBUserPassword = Helpers.ReadInput("Enter your: " + dbGrafanaDBUser + " password otherwise default to [0P@ssWord!!!]");
            if (string.IsNullOrEmpty(dbGrafanaDBUserPassword))
                dbGrafanaDBUserPassword = "0P@ssWord!!!";

            grafanaDefaultAdminPassword = Helpers.ReadInput("Enter your grafana default admin password (GF_SECURITY_ADMIN_PASSWORD) defaults to [0P@ssWord1234!!!]");
            if (string.IsNullOrEmpty(grafanaDefaultAdminPassword))
                grafanaDefaultAdminPassword = "0P@ssWord1234!!!";

        }
        var grafanaDBSrc = string.IsNullOrEmpty(dbSource) ? "tcp:mssql,1433" : dbSource;
        grafanaDBSrc = grafanaDBSrc.Replace("tcp:", "");
        grafanaDBSrc = grafanaDBSrc.Replace(",", ":");
        _grafanaOverrideValues = new Dictionary<string, string>
        {
            //["GF_INSTALL_PLUGINS"] = "\"grafana-clock-panel,grafana-simple-json-datasource,grafana-worldmap-panel,grafana-piechart-panel\"",
            //["GF_SERVER_SERVE_FROM_SUB_PATH"] = "true",
            //["GF_SERVER_ROOT_URL"] = "\"%(protocol)s://%(domain)s/grafana\"",
            ["BSAFE_DB_URL"] = grafanaDBSrc,
            ["BSAFE_DB_NAME"] = string.IsNullOrEmpty(dbCatalog) ? "vault" : dbCatalog,
            ["GF_SECURITY_ADMIN_PASSWORD"] = grafanaDefaultAdminPassword
            //["GRAFANA_DB_USER"] = "${globalSettings__grafana__dBUser}",
            //["GRAFANA_DB_PASSWORD"] = "${globalSettings__grafana__dBUserPassword}"
        };

        var baseServiceInfo = new Dictionary<string, string>
        {
            ["globalSettings__baseServiceUri__vault"] = _context.Config.Url,
            ["globalSettings__baseServiceUri__cloudRegion"] = _context.Install?.CloudRegion.ToString(),
        };

        dbPassword = string.IsNullOrEmpty(dbPassword) ? Helpers.SecureRandomString(32) : dbPassword;

        var dbConnectionInfo = new Dictionary<string, string>();
        if (dbProvider.CompareTo("sqlserver") == 0)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
            {
                DataSource = string.IsNullOrEmpty(dbSource) ? "tcp:mssql,1433" : dbSource,
                InitialCatalog = string.IsNullOrEmpty(dbCatalog) ? "vault" : dbCatalog,
                UserID = string.IsNullOrEmpty(dbUser) ? "sa" : dbUser,
                Password = dbPassword,
                MultipleActiveResultSets = false,
                Encrypt = true,
                ConnectTimeout = 30,
                TrustServerCertificate = true,
                PersistSecurityInfo = false
            };

            var dbConnectionString = builder.ConnectionString;

            dbConnectionInfo.Add("globalSettings__sqlServer__connectionString", $"\"{dbConnectionString.Replace("\"", "\\\"")}\"");
            dbConnectionInfo.Add("globalSettings__sqlServer__cryptKey", Convert.ToBase64String(Helpers.GenerateNewKey()) );
            dbConnectionInfo.Add("globalSettings__sqlServer__authKey", Convert.ToBase64String(Helpers.GenerateNewKey()) );
        }
        else if (dbProvider.CompareTo("postgres") == 0)
        {
            // Create a new instance of NpgsqlConnectionStringBuilder
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = string.IsNullOrEmpty(dbSource) ? "postgres" : dbSource,         // e.g., "localhost"
                Port = string.IsNullOrEmpty(dbPgSqlPort) ? 5432 : int.Parse(dbPgSqlPort), // Default port for PostgreSQL
                Database = string.IsNullOrEmpty(dbCatalog) ? "vault" : dbCatalog,  // Your database name
                Username = string.IsNullOrEmpty(dbUser) ? "postgres" : dbUser,        // Your username
                Password = dbPassword,        // Your password
                SslMode = SslMode.Prefer,          // SSL mode (None, Prefer, Require, etc.)
                TrustServerCertificate = true      // Trust the server certificate (true/false)
            };

            var dbConnectionString = connectionStringBuilder.ConnectionString;
            dbConnectionInfo.Add("globalSettings__databaseProvider", dbProvider); // this one only needed for non sql server back ends
            dbConnectionInfo.Add("globalSettings__postgreSql__connectionString", $"\"{dbConnectionString.Replace("\"", "\\\"")}\"");
            dbConnectionInfo.Add("globalSettings__postgreSql__cryptKey", Convert.ToBase64String(Helpers.GenerateNewKey()));
            dbConnectionInfo.Add("globalSettings__postgreSql__authKey", Convert.ToBase64String(Helpers.GenerateNewKey()));
        }

        var tmp_globalOverrideValues = new Dictionary<string, string>
        {
            ["globalSettings__identityServer__certificatePassword"] = _context.Install?.IdentityCertPassword,
            ["globalSettings__internalIdentityKey"] = _context.Stub ? "RANDOM_IDENTITY_KEY" :
                Helpers.SecureRandomString(64, alpha: true, numeric: true),
            ["globalSettings__oidcIdentityClientKey"] = _context.Stub ? "RANDOM_IDENTITY_KEY" :
                Helpers.SecureRandomString(64, alpha: true, numeric: true),
            ["globalSettings__duo__aKey"] = _context.Stub ? "RANDOM_DUO_AKEY" :
                Helpers.SecureRandomString(64, alpha: true, numeric: true),
            ["globalSettings__hypr__sKey"] = _context.Stub ? "RANDOM_HYPR_SKEY" :
                Helpers.SecureRandomString(64, alpha: true, numeric: true),
            ["globalSettings__installation__id"] = _context.Install?.InstallationId.ToString(),
            ["globalSettings__installation__key"] = _context.Install?.InstallationKey,
            ["globalSettings__yubico__clientId"] = "REPLACE",
            ["globalSettings__yubico__key"] = "REPLACE",
            ["globalSettings__mail__replyToEmail"] = $"no-reply@{_context.Config.Domain}",
            ["globalSettings__mail__smtp__host"] = "mailrelay",
            ["globalSettings__mail__smtp__port"] = "1025",
            ["globalSettings__mail__smtp__ssl"] = "false",
            ["globalSettings__mail__smtp__username"] = "REPLACE",
            ["globalSettings__mail__smtp__password"] = "REPLACE",
            ["globalSettings__disableUserRegistration"] = "false",
            ["globalSettings__hibpApiKey"] = "REPLACE",

            ["globalSettings__disableEmailNewDevice"] = "true",
            ["globalSettings__twoFactorAuth__emailOnNewDeviceLogin"] = "false",

            ["adminSettings__admins"] = string.Empty,
        };

        _globalOverrideValues = new Dictionary<string, string>();
        foreach (var kvp in baseServiceInfo)
        {
            _globalOverrideValues[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in dbConnectionInfo)
        {
            _globalOverrideValues[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in tmp_globalOverrideValues)
        {
            _globalOverrideValues[kvp.Key] = kvp.Value;
        }

        if (!_context.Config.PushNotifications)
        {
            _globalOverrideValues.Add("globalSettings__pushRelayBaseUri", "REPLACE");
        }

        if (dbProvider.CompareTo("sqlserver") == 0)
        {
            _mssqlOverrideValues = new Dictionary<string, string>
            {
                ["SA_PASSWORD"] = dbPassword,
                ["DATABASE"] = _context.Install?.Database ?? "vault"
            };
        }

        if (dbProvider.CompareTo("postgres") == 0)
        {
            _pgsqlOverrideValues = new Dictionary<string, string>
            {
                ["POSTGRES_USER"] = dbUser,
                ["POSTGRES_PASSWORD"] = dbPassword //,
                //["POSTGRES_DB"] = _context.Install?.Database ?? "vault"
            };
        }
        else { }

        _keyConnectorOverrideValues = new Dictionary<string, string>
        {
            ["keyConnectorSettings__webVaultUri"] = _context.Config.Url,
            ["keyConnectorSettings__identityServerUri"] = "http://identity:5000",
            ["keyConnectorSettings__database__provider"] = "json",
            ["keyConnectorSettings__database__jsonFilePath"] = "/etc/bitwarden/key-connector/data.json",
            ["keyConnectorSettings__rsaKey__provider"] = "certificate",
            ["keyConnectorSettings__certificate__provider"] = "filesystem",
            ["keyConnectorSettings__certificate__filesystemPath"] = "/etc/bitwarden/key-connector/bwkc.pfx",
            ["keyConnectorSettings__certificate__filesystemPassword"] = Helpers.SecureRandomString(32, alpha: true, numeric: true),
        };

        //_awsSNSOverrideValues = new Dictionary<string, string>
        {
            _globalOverrideValues.Add("# globalSettings__amazon__useSESNativeEmail", "false");
            _globalOverrideValues.Add("globalSettings__amazon__accessKeyId", "AWSKEYID");
            _globalOverrideValues.Add("globalSettings__amazon__accessKeySecret", "AWSSECRET");
            _globalOverrideValues.Add("globalSettings__amazon__region", "replaceme");
            _globalOverrideValues.Add("globalSettings__amazon__sNSPlatformARNAndroid", "arn:aws:sns:us-east-1:1234567890:app/GCM/BravuraSafeAndroid_REPLACEWHOLELINE");
            _globalOverrideValues.Add("globalSettings__amazon__sNSPlatformARNIOS", "arn:aws:sns:us-east-1:1234567890:app/APNS/BravuraSafe_iOSREPLACEWHOLELINE");
            _globalOverrideValues.Add("globalSettings__amazon__sNSTopicARN", "arn:aws:sns:us-east-1:1234567890:BravuraSafeTestTopic_ReplaceWholeLine");
        }

        //grafana settings
        {
            _globalOverrideValues.Add("globalSettings__grafana__dBUser", dbGrafanaDBUser);
            _globalOverrideValues.Add("globalSettings__grafana__dBUserPassword", dbGrafanaDBUserPassword);
        }

    }

    private void LoadExistingValues(IDictionary<string, string> _values, string file)
    {
        if (!File.Exists(file))
        {
            return;
        }

        var fileLines = File.ReadAllLines(file);
        foreach (var line in fileLines)
        {
            if (!line.Contains("="))
            {
                continue;
            }

            var value = string.Empty;
            var lineParts = line.Split("=", 2);
            if (lineParts.Length < 1)
            {
                continue;
            }

            if (lineParts.Length > 1)
            {
                value = lineParts[1];
            }

            if (_values.ContainsKey(lineParts[0]))
            {
                _values[lineParts[0]] = value;
            }
            else
            {
                _values.Add(lineParts[0], value.Replace("\\\"", "\""));
            }
        }
    }

    private void Build()
    {
        var template = Helpers.ReadTemplate("EnvironmentFile");

        Helpers.WriteLine(_context, "Building docker environment files.");
        Directory.CreateDirectory("/bitwarden/docker/");
        using (var sw = File.CreateText("/bitwarden/docker/global.env"))
        {
            sw.Write(template(new TemplateModel(_globalValues)));
        }
        Helpers.Exec("chmod 600 /bitwarden/docker/global.env");

        if (_mssqlOverrideValues.Count > 0) // know this seems weird by only generate base file if actually will override them later
        {
            using (var sw = File.CreateText("/bitwarden/docker/mssql.env"))
            {
                sw.Write(template(new TemplateModel(_mssqlValues)));
            }
            Helpers.Exec("chmod 600 /bitwarden/docker/mssql.env");
        }

        Helpers.WriteLine(_context, "Building docker environment override files.");
        Directory.CreateDirectory("/bitwarden/env/");
        using (var sw = File.CreateText("/bitwarden/env/global.override.env"))
        {
            sw.Write(template(new TemplateModel(_globalOverrideValues)));
        }
        Helpers.Exec("chmod 600 /bitwarden/env/global.override.env");

        if (_mssqlOverrideValues.Count > 0)
        {
            using (var sw = File.CreateText("/bitwarden/env/mssql.override.env"))
            {
                sw.Write(template(new TemplateModel(_mssqlOverrideValues)));
            }
            Helpers.Exec("chmod 600 /bitwarden/env/mssql.override.env");
        }

        if (_pgsqlOverrideValues.Count > 0)
        {
            using (var sw = File.CreateText("/bitwarden/docker/pgsql.env"))
            {
                sw.Write(template(new TemplateModel(_pgsqlValues)));
            }
            Helpers.Exec("chmod 600 /bitwarden/docker/pgsql.env");

            using (var sw = File.CreateText("/bitwarden/env/pgsql.override.env"))
            {
                sw.Write(template(new TemplateModel(_pgsqlOverrideValues)));
            }
            Helpers.Exec("chmod 600 /bitwarden/env/pgsql.override.env");
        }

        if (_context.Config.EnableKeyConnector)
        {
            using (var sw = File.CreateText("/bitwarden/env/key-connector.override.env"))
            {
                sw.Write(template(new TemplateModel(_keyConnectorOverrideValues)));
            }

            Helpers.Exec("chmod 600 /bitwarden/env/key-connector.override.env");
        }

        // Empty uid env file. Only used on Linux hosts.
        if (!File.Exists("/bitwarden/env/uid.env"))
        {
            using (var sw = File.CreateText("/bitwarden/env/uid.env")) { }
        }

        using (var sw = File.CreateText("/bitwarden/env/grafana.override.env"))
        {
            sw.Write(template(new TemplateModel(_grafanaOverrideValues)));
        }
        Helpers.Exec("chmod 600 /bitwarden/env/grafana.override.env");
    }

    public class TemplateModel
    {
        public TemplateModel(IEnumerable<KeyValuePair<string, string>> variables)
        {
            Variables = variables.Select(v => new Kvp { Key = v.Key, Value = v.Value });
        }

        public IEnumerable<Kvp> Variables { get; set; }

        public class Kvp
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }
    }
}

using Microsoft.Data.SqlClient;

namespace MecaniCar360.Concurrency;

public sealed class IsolatedDatabaseGuard
{
    public const string Server = @"BOOK-JKTL5UI49E\CABRALPEDRO";
    public const string Prefix = "MecaniCar360_Concurrency_";
    public string RunId { get; }
    public string Database => Prefix + RunId;
    public string ConnectionString { get; }

    private IsolatedDatabaseGuard(string connection, string runId)
    {
        if (!Guid.TryParseExact(runId, "N", out var id) || id == Guid.Empty || runId != id.ToString("N"))
            throw new InvalidOperationException("A nonempty lowercase GUID N run ID is required.");
        RunId = runId;
        var cs = new SqlConnectionStringBuilder(connection);
        if (cs.DataSource != Server || cs.InitialCatalog != Database ||
            cs.InitialCatalog.Equals("MecaniCar360", StringComparison.OrdinalIgnoreCase) ||
            new[] { "master", "model", "msdb", "tempdb" }.Contains(cs.InitialCatalog, StringComparer.OrdinalIgnoreCase) ||
            cs.AttachDBFilename.Length != 0 || cs.UserInstance || cs.FailoverPartner.Length != 0 ||
            !cs.IntegratedSecurity || cs.UserID.Length != 0 || cs.Password.Length != 0 || cs.Enlist ||
            cs.MultipleActiveResultSets || cs.ApplicationIntent != ApplicationIntent.ReadWrite)
            throw new InvalidOperationException("Isolated SQL target rejected. No fallback configuration is allowed.");
        cs.Pooling = false;
        cs.ConnectTimeout = 5;
        cs.ApplicationName = "MC360-Concurrency-" + RunId;
        ConnectionString = cs.ConnectionString;
    }

    public static IsolatedDatabaseGuard FromEnvironment() => new(
        Environment.GetEnvironmentVariable("MC360_CONCURRENCY_CONNECTION")
            ?? throw new InvalidOperationException("MC360_CONCURRENCY_CONNECTION is required."),
        Environment.GetEnvironmentVariable("MC360_CONCURRENCY_RUN_ID") ?? "");

    // Setup only: caller must explicitly opt in before invoking this method.
    public SqlConnection NewMasterConnection()
    {
        var cs = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" };
        return new SqlConnection(cs.ConnectionString);
    }

    public async Task CheckEffectiveAsync(SqlConnection connection, bool master = false)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')), DB_NAME()";
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || !string.Equals(reader.GetString(0), Server, StringComparison.OrdinalIgnoreCase) ||
            reader.GetString(1) != (master ? "master" : Database))
            throw new InvalidOperationException("Effective server/database mismatch; aborting.");
    }

    public async Task CheckRunnerAsync(SqlConnection connection)
    {
        await CheckEffectiveAsync(connection);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT
              (SELECT CONVERT(nvarchar(128),value) FROM sys.extended_properties WHERE class=0 AND name=N'MC360_Concurrency_Run'),
              (SELECT CONVERT(nvarchar(128),value) FROM sys.extended_properties WHERE class=0 AND name=N'MC360_Concurrency_PreparedBy'),
              CONVERT(nvarchar(128),ORIGINAL_LOGIN()),
              IS_SRVROLEMEMBER('sysadmin'), HAS_PERMS_BY_NAME(NULL,NULL,'CONTROL SERVER'),
              HAS_PERMS_BY_NAME(NULL,NULL,'ALTER ANY DATABASE'), HAS_PERMS_BY_NAME(NULL,NULL,'CREATE ANY DATABASE'),
              IS_MEMBER('db_owner'), HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CONTROL'),
              HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER'), HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CREATE TABLE'),
              HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','BACKUP DATABASE'), HAS_DBACCESS(N'MecaniCar360')
            """;
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || reader.IsDBNull(0) || reader.GetString(0) != RunId ||
            reader.IsDBNull(1) || reader.GetString(1).Equals(reader.GetString(2), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Missing preparation marker or setup identity reused by runner.");
        for (var i = 3; i < reader.FieldCount; i++)
            if (reader.IsDBNull(i) || Convert.ToInt32(reader.GetValue(i)) != 0)
                throw new InvalidOperationException("Runner must have no administrative privileges or development database access.");
        await reader.CloseAsync();
        cmd.CommandText = "SELECT HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION')";
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("VIEW DEFINITION is required to verify every foreign key.");
    }
}

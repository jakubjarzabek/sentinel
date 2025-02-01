using System.Globalization;
using Sentinel.Interfaces.Providers;

namespace Sentinel.Providers.DbMonitor;

public class DbMonitoringProviderSettings : IDbMonitoringProviderSettings
{
    public string Name { get; }

    public string Summary => string.Format(CultureInfo.InvariantCulture, "Monitor the db for new log entries");

    public IProviderInfo Info { get; }

    public string ConnectionString { get; private set; }

    public string TableName { get; private set;}

    public int RefreshInSeconds { get; private set;}

    public bool LoadExistingContent { get; private set;}

    public DbMonitoringProviderSettings(string name, IProviderInfo info, string connectionString, string tableName, int refreshPeriod, bool loadExistingContent)
    {
        Name = name;
        Info = info;
        ConnectionString = connectionString;
        TableName = tableName;
        RefreshInSeconds = refreshPeriod;
        LoadExistingContent = loadExistingContent;
    }

    public void Update(string connectionString, string tableName, int refresh, bool loadExisting)
    {
        ConnectionString = connectionString;
        TableName = tableName;
        RefreshInSeconds = refresh;
        LoadExistingContent = loadExisting;
    }
}
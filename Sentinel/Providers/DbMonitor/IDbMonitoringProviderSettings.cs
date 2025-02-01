using Sentinel.Interfaces.Providers;

namespace Sentinel.Providers.DbMonitor;

public interface IDbMonitoringProviderSettings : IProviderSettings
{
    string ConnectionString { get; }

    string TableName { get; }

    int RefreshInSeconds { get; }

    bool LoadExistingContent { get; }

    void Update(string connectionString, string tableName, int refresh, bool loadExisting);
}
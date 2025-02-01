using Sentinel.Interfaces.Providers;

namespace Sentinel.Providers.DbMonitor;

public interface IDbMonitoringProviderSettings : IProviderSettings
{
    string FileName { get; }

    int RefreshPeriod { get; }

    bool LoadExistingContent { get; }

    string MessageDecoder { get; set; }

    void Update(string fileName, int refresh, bool loadExisting);
}
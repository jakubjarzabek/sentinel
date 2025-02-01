using Sentinel.Interfaces.Providers;

namespace Sentinel.Providers.FileMonitor;

public interface IFileMonitoringProviderSettings : IProviderSettings
{
    string FileName { get; }

    int RefreshPeriod { get; }

    bool LoadExistingContent { get; }

    string MessageDecoder { get; set; }

    void Update(string fileName, int refresh, bool loadExisting);
}
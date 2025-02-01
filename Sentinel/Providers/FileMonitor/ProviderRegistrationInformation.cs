using Sentinel.Interfaces.Providers;

namespace Sentinel.Providers.FileMonitor;

public class ProviderRegistrationInformation(IProviderInfo providerInfo) : IProviderRegistrationRecord
{
    public Guid Identifier => Info.Identifier;

    public IProviderInfo Info { get; } = providerInfo;

    public Type Settings => typeof(FileMonitorProviderPage);

    public Type Implementer => typeof(FileMonitoringProvider);
}
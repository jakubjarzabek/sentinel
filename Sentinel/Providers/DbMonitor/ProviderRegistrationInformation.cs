using Sentinel.Interfaces.Providers;

namespace Sentinel.Providers.DbMonitor;

public class ProviderRegistrationInformation(IProviderInfo providerInfo) : IProviderRegistrationRecord
{
    public Guid Identifier => Info.Identifier;

    public IProviderInfo Info { get; } = providerInfo;

    public Type Settings => typeof(DbMonitorProviderPage);

    public Type Implementer => typeof(DbMonitoringProvider);
}
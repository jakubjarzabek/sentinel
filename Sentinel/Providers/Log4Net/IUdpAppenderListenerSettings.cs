using Sentinel.Interfaces.Providers;

namespace Sentinel.Providers.Log4Net;

public interface IUdpAppenderListenerSettings : IProviderSettings
{
    int Port { get; set; }
}
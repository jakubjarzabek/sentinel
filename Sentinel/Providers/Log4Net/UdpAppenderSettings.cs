using Sentinel.Interfaces.Providers;

namespace Sentinel.Providers.Log4Net;

public class UdpAppenderSettings : IUdpAppenderListenerSettings
{
    public UdpAppenderSettings()
    {
        Name = "Log4net UDP Appender";
        Info = Log4NetProvider.ProviderRegistrationInformation.Info;
    }

    public UdpAppenderSettings(IProviderSettings providerInfo)
    {
        Name = providerInfo.Name;
        Info = providerInfo.Info;
    }

    public string Name { get; set; }

    public string Summary => $"{Name}: Listens on port {Port}";

    public IProviderInfo Info { get; set; }

    public int Port { get; set; }
}
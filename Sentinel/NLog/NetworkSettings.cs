﻿namespace Sentinel.NLog;

using System.Runtime.Serialization;

[DataContract]
public class NetworkSettings : ProviderSettings, INLogAppenderSettings
{
    public NetworkProtocol Protocol { get; set; } = NetworkProtocol.Udp;

    public int Port { get; set; } = 9999;

    public override string Summary
    {
        get
        {
            return $"{Name}: Listens on {Protocol} port {Port}";
        }
    }
}
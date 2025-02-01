namespace Sentinel.StartUp;

using CommandLine;

[Verb("nlog", HelpText = "Use nlog listener")]
public class NLogOptions : IOptions
{
    [Option('t', "tcp", SetName = "protocols")]
    public bool IsTcp
    {
        get => !IsUdp;
        set => IsUdp = !value;
    }

    public int Port { get; set; } = 9999;

    public bool IsUdp { get; set; } = true;
}
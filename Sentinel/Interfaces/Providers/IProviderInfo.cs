using System.Runtime.Serialization;

namespace Sentinel.Interfaces.Providers;

public interface IProviderInfo
{
    [DataMember]
    Guid Identifier { get; }

    [DataMember]
    string Name { get; }

    [DataMember]
    string Description { get; }
}
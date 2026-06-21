using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Bunny.Storage;

[AspireExportIgnore(Reason = "TypeScript AppHosts use parameter builders instead.")]
public sealed class BunnyStorageValue
{
    private BunnyStorageValue(string literalValue) => LiteralValue = literalValue;

    private BunnyStorageValue(ParameterResource parameter) => Parameter = parameter;

    public string? LiteralValue { get; }

    public ParameterResource? Parameter { get; }

    public static BunnyStorageValue FromString(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new BunnyStorageValue(value);
    }

    public static BunnyStorageValue FromParameter(ParameterResource parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return new BunnyStorageValue(parameter);
    }

    public static BunnyStorageValue FromParameter(IResourceBuilder<ParameterResource> parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return FromParameter(parameter.Resource);
    }

    public static implicit operator BunnyStorageValue(string value) => FromString(value);
}

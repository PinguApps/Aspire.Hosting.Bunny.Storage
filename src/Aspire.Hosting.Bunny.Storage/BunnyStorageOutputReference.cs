using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting.Bunny.Storage;

[AspireExport("pinguapps.bunny.storage.outputReference", ExposeProperties = false, ExposeMethods = false)]
public sealed class BunnyStorageOutputReference : IExpressionValue, IValueProvider, IManifestExpressionProvider, IValueWithReferences
{
    private readonly AzureBlobStorageContainerResource _resource;
    private string? _value;

    internal BunnyStorageOutputReference(AzureBlobStorageContainerResource resource, string name, bool secret = false)
    {
        _resource = resource;
        Name = name;
        Secret = secret;
        ValueExpression = $"{{{resource.Name}.outputs.{name}}}";
    }

    [AspireExportIgnore(Reason = "Output metadata is not part of the TypeScript authoring surface.")]
    public string Name { get; }

    [AspireExportIgnore(Reason = "Output metadata is not part of the TypeScript authoring surface.")]
    public bool Secret { get; }

    [AspireExportIgnore(Reason = "Reference mechanics are consumed by Aspire.")]
    public IEnumerable<object> References => [_resource];

    [AspireExportIgnore(Reason = "Reference mechanics are consumed by Aspire.")]
    public string ValueExpression { get; }

    [AspireExportIgnore(Reason = "Reference mechanics are consumed by Aspire.")]
    public ReferenceExpression AsReferenceExpression() => ReferenceExpression.Create($"{this}");

    [AspireExportIgnore(Reason = "Reference values are resolved by Aspire.")]
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _value is null
            ? throw new InvalidOperationException("The Bunny Storage output has not been populated by the deployment pipeline.")
            : ValueTask.FromResult<string?>(_value);
    }

    [AspireExportIgnore(Reason = "Reference values are resolved by Aspire.")]
    public ValueTask<string?> GetValueAsync(ValueProviderContext context, CancellationToken cancellationToken)
    {
        return GetValueAsync(cancellationToken);
    }

    internal void SetValue(string? value) => _value = value ?? string.Empty;
}

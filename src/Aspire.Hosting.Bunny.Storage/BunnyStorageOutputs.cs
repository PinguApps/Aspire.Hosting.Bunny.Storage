using Aspire.Hosting.Azure;
using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage;

[AspireExport("pinguapps.bunny.storage.outputs", ExposeProperties = true, ExposeMethods = false)]
public sealed class BunnyStorageOutputs
{
    internal BunnyStorageOutputs(AzureBlobStorageContainerResource resource)
    {
        StorageZoneId = new(resource, BunnyStorageOutputNames.StorageZoneId);
        StorageZoneName = new(resource, BunnyStorageOutputNames.StorageZoneName);
        StorageEndpoint = new(resource, BunnyStorageOutputNames.StorageEndpoint);
        AccessKey = new(resource, BunnyStorageOutputNames.AccessKey, secret: true);
        PullZoneId = new(resource, BunnyStorageOutputNames.PullZoneId);
        PullZoneName = new(resource, BunnyStorageOutputNames.PullZoneName);
        PublicBaseUrl = new(resource, BunnyStorageOutputNames.PublicBaseUrl);
        Properties = [StorageZoneId, StorageZoneName, StorageEndpoint, AccessKey, PullZoneId, PullZoneName, PublicBaseUrl];
    }

    public BunnyStorageOutputReference StorageZoneId { get; }

    public BunnyStorageOutputReference StorageZoneName { get; }

    public BunnyStorageOutputReference StorageEndpoint { get; }

    public BunnyStorageOutputReference AccessKey { get; }

    public BunnyStorageOutputReference PullZoneId { get; }

    public BunnyStorageOutputReference PullZoneName { get; }

    public BunnyStorageOutputReference PublicBaseUrl { get; }

    [AspireExportIgnore(Reason = "TypeScript AppHosts consume named output properties directly.")]
    public IReadOnlyList<BunnyStorageOutputReference> Properties { get; }

    [AspireExportIgnore(Reason = "Output secret classification is implementation metadata.")]
    public static bool IsSecret(string outputName)
    {
        ArgumentNullException.ThrowIfNull(outputName);
        return string.Equals(outputName, BunnyStorageOutputNames.AccessKey, StringComparison.Ordinal);
    }

    internal void Populate(BunnyStorageZoneDetails zone, BunnyPullZoneDetails? pullZone, string storageEndpoint, string publicBaseUrl)
    {
        StorageZoneId.SetValue(zone.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        StorageZoneName.SetValue(zone.Name);
        StorageEndpoint.SetValue(storageEndpoint);
        AccessKey.SetValue(zone.Password);
        PullZoneId.SetValue(pullZone?.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        PullZoneName.SetValue(pullZone?.Name);
        PublicBaseUrl.SetValue(publicBaseUrl);
    }
}

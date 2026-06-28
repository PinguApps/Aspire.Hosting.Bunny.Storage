using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting.Bunny.Storage;

public static class BunnyStorageResourceExtensions
{
    internal static BunnyStorageDeploymentState? GetBunnyStorageDeploymentState(this AzureBlobStorageContainerResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        BunnyStorageDeploymentAnnotation? annotation = resource.Annotations.OfType<BunnyStorageDeploymentAnnotation>().LastOrDefault();
        return annotation is null
            ? null
            : new BunnyStorageDeploymentState(annotation.StorageZoneName, annotation.ApiKey, annotation.OwnershipMode, annotation.Options);
    }

    [AspireExport("pinguapps.bunny.storage.getBunnyStorageOutputs", MethodName = "getBunnyStorageOutputs")]
    public static BunnyStorageOutputs? GetBunnyStorageOutputs(this IResourceBuilder<AzureBlobStorageContainerResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Resource.TryGetBunnyStorageOutputs();
    }

    internal static BunnyStorageOutputs? TryGetBunnyStorageOutputs(this AzureBlobStorageContainerResource resource)
    {
        return resource.Annotations.OfType<BunnyStorageOutputsAnnotation>().LastOrDefault()?.Outputs;
    }
}

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Bunny.Storage;

internal sealed class BunnyStorageDeploymentAnnotation : IResourceAnnotation
{
    public BunnyStorageDeploymentAnnotation(
        BunnyStorageValue storageZoneName,
        BunnyStorageValue apiKey,
        BunnyStorageOwnershipMode ownershipMode,
        BunnyStorageDeploymentOptions options)
    {
        StorageZoneName = storageZoneName;
        ApiKey = apiKey;
        OwnershipMode = ownershipMode;
        Options = new BunnyStorageDeploymentOptions(options);
    }

    public BunnyStorageValue StorageZoneName { get; }

    public BunnyStorageValue ApiKey { get; }

    public BunnyStorageOwnershipMode OwnershipMode { get; }

    public BunnyStorageDeploymentOptions Options { get; }
}

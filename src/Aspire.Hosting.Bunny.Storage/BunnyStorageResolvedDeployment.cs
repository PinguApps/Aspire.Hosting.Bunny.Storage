using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage;

public sealed record BunnyStorageResolvedDeployment
{
    public BunnyStorageResolvedDeployment(
        string storageZoneName,
        BunnyStorageOwnershipMode ownershipMode,
        BunnyStorageManagementCredentials managementCredentials,
        BunnyStorageDeploymentOptions options)
    {
        StorageZoneName = storageZoneName;
        OwnershipMode = ownershipMode;
        ManagementCredentials = managementCredentials;
        Options = options;
    }

    public string StorageZoneName { get; }

    public BunnyStorageOwnershipMode OwnershipMode { get; }

    public BunnyStorageManagementCredentials ManagementCredentials { get; }

    public BunnyStorageDeploymentOptions Options { get; }

    public string StorageEndpoint => Options.Region.GetStorageEndpoint();
}

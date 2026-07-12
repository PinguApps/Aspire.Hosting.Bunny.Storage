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

    public string StorageZoneName { get; init; }

    public BunnyStorageOwnershipMode OwnershipMode { get; init; }

    public BunnyStorageManagementCredentials ManagementCredentials { get; init; }

    public BunnyStorageDeploymentOptions Options { get; init; }

    public string StorageEndpoint => Options.Region.GetStorageEndpoint();

    public void Deconstruct(
        out string storageZoneName,
        out BunnyStorageOwnershipMode ownershipMode,
        out BunnyStorageManagementCredentials managementCredentials,
        out BunnyStorageDeploymentOptions options)
    {
        storageZoneName = StorageZoneName;
        ownershipMode = OwnershipMode;
        managementCredentials = ManagementCredentials;
        options = Options;
    }
}

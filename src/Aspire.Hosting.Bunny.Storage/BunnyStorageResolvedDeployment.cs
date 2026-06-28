using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage;

public sealed record BunnyStorageResolvedDeployment(
    string StorageZoneName,
    BunnyStorageOwnershipMode OwnershipMode,
    BunnyStorageManagementCredentials ManagementCredentials,
    BunnyStorageDeploymentOptions Options)
{
    public string StorageEndpoint => Options.Region.GetStorageEndpoint();
}

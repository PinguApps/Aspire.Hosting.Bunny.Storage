using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage.Deployment;

public sealed record BunnyStorageCreateFlowResult(
    BunnyStorageZoneDetails StorageZone,
    BunnyPullZoneDetails? PullZone,
    bool Created,
    BunnyStorageRemoteIdentityState RemoteIdentity);

namespace Aspire.Hosting.Bunny.Storage.Management;

public interface IBunnyStorageManagementClient
{
    Task<IReadOnlyList<BunnyStorageZoneDetails>> ListStorageZonesAsync(CancellationToken cancellationToken);

    Task<BunnyStorageZoneDetails> CreateStorageZoneAsync(string name, string region, IReadOnlyList<string> replicationRegions, CancellationToken cancellationToken);

    Task<IReadOnlyList<BunnyPullZoneDetails>> ListPullZonesAsync(CancellationToken cancellationToken);

    Task<BunnyPullZoneDetails> CreatePullZoneAsync(string name, string originUrl, long storageZoneId, CancellationToken cancellationToken);
}

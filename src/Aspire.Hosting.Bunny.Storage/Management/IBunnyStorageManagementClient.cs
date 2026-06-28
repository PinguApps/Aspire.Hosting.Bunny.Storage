namespace Aspire.Hosting.Bunny.Storage.Management;

public interface IBunnyStorageManagementClient
{
    public Task<IReadOnlyList<BunnyStorageZoneDetails>> ListStorageZonesAsync(CancellationToken cancellationToken);

    public Task<BunnyStorageZoneDetails> CreateStorageZoneAsync(string name, string region, IReadOnlyList<string> replicationRegions, CancellationToken cancellationToken);

    public Task<IReadOnlyList<BunnyPullZoneDetails>> ListPullZonesAsync(CancellationToken cancellationToken);

    public Task<BunnyPullZoneDetails> CreatePullZoneAsync(string name, string originUrl, long storageZoneId, CancellationToken cancellationToken);
}

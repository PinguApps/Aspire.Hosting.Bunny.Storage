using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage.Deployment;

public sealed class BunnyStorageRemoteIdentityResolver
{
    private readonly IBunnyStorageManagementClient _client;

    public BunnyStorageRemoteIdentityResolver(IBunnyStorageManagementClient client) => _client = client;

    public async Task<BunnyStorageRemoteIdentityStateResult> ResolveAsync(
        string storageZoneName,
        BunnyStorageRemoteIdentityState? cachedIdentity,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<BunnyStorageZoneDetails> zones = await _client.ListStorageZonesAsync(cancellationToken).ConfigureAwait(false);
        BunnyStorageZoneDetails? byName = zones.FirstOrDefault(zone => string.Equals(zone.Name, storageZoneName, StringComparison.OrdinalIgnoreCase));

        if (cachedIdentity is null)
        {
            return new BunnyStorageRemoteIdentityStateResult(byName, ResolvedFromCachedIdentity: false);
        }

        BunnyStorageZoneDetails? byId = zones.FirstOrDefault(zone =>
            string.Equals(zone.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), cachedIdentity.ProviderStorageZoneId, StringComparison.Ordinal));

        if (byId is not null
            && !string.Equals(byId.Name, storageZoneName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Bunny Storage resource was previously deployed as '{cachedIdentity.StorageZoneName}' with id '{cachedIdentity.ProviderStorageZoneId}', but the AppHost now requests '{storageZoneName}'. Rename requires explicit state cleanup.");
        }

        return new BunnyStorageRemoteIdentityStateResult(byId ?? byName, byId is not null);
    }
}

public sealed record BunnyStorageRemoteIdentityStateResult(BunnyStorageZoneDetails? StorageZone, bool ResolvedFromCachedIdentity);

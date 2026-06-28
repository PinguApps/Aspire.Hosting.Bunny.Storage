using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage.Deployment;

public sealed class BunnyStorageRemoteIdentityResolver
{
    private readonly IBunnyStorageManagementClient _client;

    public BunnyStorageRemoteIdentityResolver(IBunnyStorageManagementClient client)
    {
        _client = client;
    }

    public async Task<BunnyStorageRemoteIdentityStateResult> ResolveAsync(
        string storageZoneName,
        BunnyStorageRemoteIdentityState? cachedIdentity,
        string? deploymentStateSectionName,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<BunnyStorageZoneDetails> zones = await _client.ListStorageZonesAsync(cancellationToken).ConfigureAwait(false);
        BunnyStorageZoneDetails? byName = zones.FirstOrDefault(zone => string.Equals(zone.Name, storageZoneName, StringComparison.OrdinalIgnoreCase));

        if (cachedIdentity is null)
        {
            return new BunnyStorageRemoteIdentityStateResult(byName, resolvedFromCachedIdentity: false);
        }

        BunnyStorageZoneDetails? byId = zones.FirstOrDefault(zone =>
            string.Equals(zone.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), cachedIdentity.ProviderStorageZoneId, StringComparison.Ordinal));

        if (byId is not null
            && !string.Equals(byId.Name, storageZoneName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Bunny Storage resource was previously deployed as '{cachedIdentity.StorageZoneName}' with id '{cachedIdentity.ProviderStorageZoneId}', but the AppHost now requests '{storageZoneName}'. Rename requires explicit deployment-state cleanup{FormatSectionName(deploymentStateSectionName)}.");
        }

        if (byId is null)
        {
            throw new InvalidOperationException(
                $"Bunny Storage resource was previously deployed as '{cachedIdentity.StorageZoneName}' with id '{cachedIdentity.ProviderStorageZoneId}', but that storage zone no longer exists. Delete the deployment-state section{FormatSectionName(deploymentStateSectionName)} before adopting or recreating a different Bunny Storage zone.");
        }

        return new BunnyStorageRemoteIdentityStateResult(byId, resolvedFromCachedIdentity: true);
    }

    private static string FormatSectionName(string? deploymentStateSectionName)
    {
        return string.IsNullOrWhiteSpace(deploymentStateSectionName)
            ? ""
            : $" '{deploymentStateSectionName}'";
    }
}

public sealed record BunnyStorageRemoteIdentityStateResult
{
    public BunnyStorageRemoteIdentityStateResult(BunnyStorageZoneDetails? storageZone, bool resolvedFromCachedIdentity)
    {
        StorageZone = storageZone;
        ResolvedFromCachedIdentity = resolvedFromCachedIdentity;
    }

    public BunnyStorageZoneDetails? StorageZone { get; init; }

    public bool ResolvedFromCachedIdentity { get; init; }

    public void Deconstruct(out BunnyStorageZoneDetails? storageZone, out bool resolvedFromCachedIdentity)
    {
        storageZone = StorageZone;
        resolvedFromCachedIdentity = ResolvedFromCachedIdentity;
    }
}

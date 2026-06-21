using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage.Deployment;

public sealed class BunnyStorageCreateFlow
{
    private readonly IBunnyStorageManagementClient _client;

    public BunnyStorageCreateFlow(IBunnyStorageManagementClient client) => _client = client;

    public async Task<BunnyStorageCreateFlowResult> ExecuteAsync(
        BunnyStorageResolvedDeployment deployment,
        BunnyStorageOwnershipResolutionResult ownership,
        CancellationToken cancellationToken)
    {
        BunnyStorageZoneDetails zone;
        bool created;
        if (ownership.Action == BunnyStorageOwnershipResolutionAction.Create)
        {
            string[] replicationRegions = [.. deployment.Options.ReplicationRegions.Select(region => region.ToProviderCode())];
            zone = await _client
                .CreateStorageZoneAsync(deployment.StorageZoneName, deployment.Options.Region.ToProviderCode(), replicationRegions, cancellationToken)
                .ConfigureAwait(false);
            created = true;
        }
        else
        {
            zone = ownership.ExistingZone ?? throw new InvalidOperationException("Ownership resolution selected an existing Bunny Storage zone but did not provide one.");
            created = false;
        }

        BunnyPullZoneDetails? pullZone = await EnsurePullZoneAsync(deployment, zone, cancellationToken).ConfigureAwait(false);
        BunnyStorageRemoteIdentityState remoteIdentity = new(
            zone.Name,
            zone.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));

        return new BunnyStorageCreateFlowResult(zone, pullZone, created, remoteIdentity);
    }

    private async Task<BunnyPullZoneDetails?> EnsurePullZoneAsync(
        BunnyStorageResolvedDeployment deployment,
        BunnyStorageZoneDetails zone,
        CancellationToken cancellationToken)
    {
        if (!deployment.Options.CreatePullZone)
        {
            return zone.PullZones?.FirstOrDefault();
        }

        string pullZoneName = deployment.Options.PullZoneName
            ?? throw new InvalidOperationException("Bunny Storage pull-zone creation requires PullZoneName.");
        IReadOnlyList<BunnyPullZoneDetails> pullZones = await _client.ListPullZonesAsync(cancellationToken).ConfigureAwait(false);
        BunnyPullZoneDetails? existing = pullZones.FirstOrDefault(zone =>
            string.Equals(zone.Name, pullZoneName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        string originUrl = $"{deployment.StorageEndpoint.TrimEnd('/')}/{Uri.EscapeDataString(zone.Name)}/";
        return await _client.CreatePullZoneAsync(pullZoneName, originUrl, zone.Id, cancellationToken).ConfigureAwait(false);
    }
}

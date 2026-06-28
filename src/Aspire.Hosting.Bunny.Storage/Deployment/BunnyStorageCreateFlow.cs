using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage.Deployment;

public sealed class BunnyStorageCreateFlow
{
    private static readonly TimeSpan[] _storageZoneDeletionRetryDelays =
    [
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(30),
    ];

    private readonly IBunnyStorageManagementClient _client;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly IReadOnlyList<TimeSpan> _storageZoneDeletionDelays;
    private readonly Func<BunnyStorageRemoteIdentityState, CancellationToken, Task>? _remoteIdentityCreatedAsync;

    public BunnyStorageCreateFlow(IBunnyStorageManagementClient client)
        : this(client, static (delay, cancellationToken) => Task.Delay(delay, cancellationToken), _storageZoneDeletionRetryDelays, remoteIdentityCreatedAsync: null)
    {
    }

    internal BunnyStorageCreateFlow(
        IBunnyStorageManagementClient client,
        Func<BunnyStorageRemoteIdentityState, CancellationToken, Task>? remoteIdentityCreatedAsync)
        : this(client, static (delay, cancellationToken) => Task.Delay(delay, cancellationToken), _storageZoneDeletionRetryDelays, remoteIdentityCreatedAsync)
    {
    }

    internal BunnyStorageCreateFlow(
        IBunnyStorageManagementClient client,
        Func<TimeSpan, CancellationToken, Task> delayAsync,
        IReadOnlyList<TimeSpan> storageZoneDeletionDelays,
        Func<BunnyStorageRemoteIdentityState, CancellationToken, Task>? remoteIdentityCreatedAsync = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(delayAsync);
        ArgumentNullException.ThrowIfNull(storageZoneDeletionDelays);

        _client = client;
        _delayAsync = delayAsync;
        _storageZoneDeletionDelays = storageZoneDeletionDelays;
        _remoteIdentityCreatedAsync = remoteIdentityCreatedAsync;
    }

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
            zone = await CreateStorageZoneAsync(deployment, replicationRegions, cancellationToken).ConfigureAwait(false);
            created = true;
        }
        else
        {
            zone = ownership.ExistingZone ?? throw new InvalidOperationException("Ownership resolution selected an existing Bunny Storage zone but did not provide one.");
            created = false;
        }

        zone = BunnyStorageReconciler.Reconcile(deployment, zone);
        BunnyStorageRemoteIdentityState remoteIdentity = new(
            zone.Name,
            zone.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (created && _remoteIdentityCreatedAsync is not null)
        {
            await _remoteIdentityCreatedAsync(remoteIdentity, cancellationToken).ConfigureAwait(false);
        }

        BunnyPullZoneDetails? pullZone = await EnsurePullZoneAsync(deployment, zone, cancellationToken).ConfigureAwait(false);
        return new BunnyStorageCreateFlowResult(zone, pullZone, created, remoteIdentity);
    }

    private async Task<BunnyStorageZoneDetails> CreateStorageZoneAsync(
        BunnyStorageResolvedDeployment deployment,
        IReadOnlyList<string> replicationRegions,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await _client
                    .CreateStorageZoneAsync(deployment.StorageZoneName, deployment.Options.Region.ToProviderCode(), replicationRegions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (BunnyStorageProviderException exception)
                when (exception.FailureKind == BunnyStorageProviderFailureKind.StorageZoneBeingDeleted
                    && attempt < _storageZoneDeletionDelays.Count)
            {
                await _delayAsync(_storageZoneDeletionDelays[attempt], cancellationToken).ConfigureAwait(false);
            }
        }
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
        string originUrl = $"{deployment.StorageEndpoint.TrimEnd('/')}/{Uri.EscapeDataString(zone.Name)}/";
        if (existing is not null)
        {
            ValidateExistingPullZone(existing, zone, originUrl);
            return existing;
        }

        return await _client.CreatePullZoneAsync(pullZoneName, originUrl, zone.Id, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateExistingPullZone(BunnyPullZoneDetails existing, BunnyStorageZoneDetails zone, string expectedOriginUrl)
    {
        if (existing.Enabled == false)
        {
            throw new InvalidOperationException(
                $"Bunny Storage pull zone '{existing.Name}' exists but is disabled.");
        }

        if (existing.Suspended == true)
        {
            throw new InvalidOperationException(
                $"Bunny Storage pull zone '{existing.Name}' exists but is suspended.");
        }

        if (existing.ZoneSecurityEnabled == true)
        {
            throw new InvalidOperationException(
                $"Bunny Storage pull zone '{existing.Name}' has URL token authentication enabled and cannot serve unsigned public object-storage URLs.");
        }

        if (existing.StorageZoneId is not null && existing.StorageZoneId != zone.Id)
        {
            throw new InvalidOperationException(
                $"Bunny Storage pull zone '{existing.Name}' is linked to storage zone id '{existing.StorageZoneId}', expected '{zone.Id}'.");
        }

        if (!string.IsNullOrWhiteSpace(existing.OriginUrl)
            && !string.Equals(NormalizeOrigin(existing.OriginUrl), NormalizeOrigin(expectedOriginUrl), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Bunny Storage pull zone '{existing.Name}' origin is '{existing.OriginUrl}', expected '{expectedOriginUrl}'.");
        }
    }

    private static string NormalizeOrigin(string originUrl)
    {
        return originUrl.TrimEnd('/') + "/";
    }
}

using Aspire.Hosting.Bunny.Storage.Management;

namespace Aspire.Hosting.Bunny.Storage.Deployment;

public static class BunnyStorageReconciler
{
    public static BunnyStorageZoneDetails Reconcile(BunnyStorageResolvedDeployment deployment, BunnyStorageZoneDetails zone)
    {
        if (!string.Equals(zone.Name, deployment.StorageZoneName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Bunny Storage immutable drift: expected storage zone name '{deployment.StorageZoneName}', found '{zone.Name}'.");
        }

        string expectedRegion = deployment.Options.Region.ToProviderCode();
        if (!string.Equals(zone.Region, expectedRegion, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Bunny Storage immutable drift: expected primary region '{expectedRegion}', found '{zone.Region}'.");
        }

        string[] expectedReplicationRegions = [.. deployment.Options.ReplicationRegions.Select(region => region.ToProviderCode()).Order(StringComparer.OrdinalIgnoreCase)];
        string[] actualReplicationRegions = [.. (zone.ReplicationRegions ?? []).Order(StringComparer.OrdinalIgnoreCase)];
        if (!expectedReplicationRegions.SequenceEqual(actualReplicationRegions, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Bunny Storage immutable drift: expected replication regions '{string.Join(", ", expectedReplicationRegions)}', found '{string.Join(", ", actualReplicationRegions)}'.");
        }

        return zone;
    }
}

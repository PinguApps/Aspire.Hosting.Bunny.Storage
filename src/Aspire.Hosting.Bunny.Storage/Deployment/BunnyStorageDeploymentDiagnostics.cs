namespace Aspire.Hosting.Bunny.Storage.Deployment;

public static class BunnyStorageDeploymentDiagnostics
{
    public static BunnyStorageDeploymentProgress CreateProgress(
        string phase,
        string message,
        string? resourceName,
        string? storageZoneName,
        string? providerStorageZoneId)
    {
        return new BunnyStorageDeploymentProgress(phase, message, resourceName, storageZoneName, providerStorageZoneId);
    }
}

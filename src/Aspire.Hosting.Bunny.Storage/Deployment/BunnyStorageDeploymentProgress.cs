namespace Aspire.Hosting.Bunny.Storage.Deployment;

public sealed record BunnyStorageDeploymentProgress(
    string Phase,
    string Message,
    string? ResourceName,
    string? StorageZoneName,
    string? ProviderStorageZoneId);

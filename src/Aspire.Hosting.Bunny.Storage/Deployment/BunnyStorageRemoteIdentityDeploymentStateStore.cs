#pragma warning disable ASPIREPIPELINES002

using System.Text.Json.Nodes;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting.Bunny.Storage.Deployment;

public sealed class BunnyStorageRemoteIdentityDeploymentStateStore
{
    private const string SectionPrefix = "Aspire.Hosting.Bunny.Storage.RemoteIdentity";
    private const string StorageZoneNameKey = "storageZoneName";
    private const string ProviderStorageZoneIdKey = "providerStorageZoneId";

    private readonly IDeploymentStateManager _stateManager;

    public BunnyStorageRemoteIdentityDeploymentStateStore(IDeploymentStateManager stateManager) => _stateManager = stateManager;

    public async Task<BunnyStorageRemoteIdentityState?> LoadAsync(string resourceName, CancellationToken cancellationToken)
    {
        DeploymentStateSection section = await _stateManager.AcquireSectionAsync(BuildSectionName(resourceName), cancellationToken).ConfigureAwait(false);
        string? name = section.Data.TryGetPropertyValue(StorageZoneNameKey, out JsonNode? nameValue) ? (string?)nameValue : null;
        string? id = section.Data.TryGetPropertyValue(ProviderStorageZoneIdKey, out JsonNode? idValue) ? (string?)idValue : null;
        return string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id)
            ? null
            : new BunnyStorageRemoteIdentityState(name, id);
    }

    public async Task SaveAsync(string resourceName, BunnyStorageRemoteIdentityState state, CancellationToken cancellationToken)
    {
        DeploymentStateSection section = await _stateManager.AcquireSectionAsync(BuildSectionName(resourceName), cancellationToken).ConfigureAwait(false);
        section.Data[StorageZoneNameKey] = state.StorageZoneName;
        section.Data[ProviderStorageZoneIdKey] = state.ProviderStorageZoneId;
        await _stateManager.SaveSectionAsync(section, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildSectionName(string resourceName) => $"{SectionPrefix}.{resourceName}";
}

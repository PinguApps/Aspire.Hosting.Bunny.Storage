#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES002

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Bunny.Storage.Deployment;
using Aspire.Hosting.Bunny.Storage.Management;
using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Bunny.Storage;

internal static class BunnyStorageDeploymentPipeline
{
    private static readonly HttpClient _managementHttpClient = new()
    {
        BaseAddress = new Uri("https://api.bunny.net/"),
    };

    private static readonly Action<ILogger, string, string?, string?, string?, Exception?> _deploymentProgress =
        LoggerMessage.Define<string, string?, string?, string?>(
            LogLevel.Information,
            new EventId(1, "BunnyStorageDeploymentProgress"),
            "{Message} Resource='{ResourceName}' StorageZone='{StorageZoneName}' StorageZoneId='{StorageZoneId}'.");

    public static async Task ExecuteAsync(AzureBlobStorageContainerResource resource, PipelineStepContext context)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(context);

        BunnyStorageDeploymentState state = resource.GetBunnyStorageDeploymentState()
            ?? throw new InvalidOperationException($"Blob container resource '{resource.Name}' is missing Bunny Storage deployment state.");

        BunnyStorageResolvedDeployment deployment = await BunnyStorageDeployTimeResolver
            .ResolveAsync(state, resource, context)
            .ConfigureAwait(false);

        IBunnyStorageManagementClient client = context.Services.GetService<IBunnyStorageManagementClient>()
            ?? new BunnyStorageManagementClient(_managementHttpClient, deployment.ManagementCredentials);

        BunnyStorageRemoteIdentityDeploymentStateStore identityStore = new(
            context.Services.GetRequiredService<IDeploymentStateManager>());
        BunnyStorageRemoteIdentityState? cachedIdentity =
            await identityStore.LoadAsync(resource.Name, context.CancellationToken).ConfigureAwait(false);

        BunnyStorageCreateFlowResult result = await ExecuteCoreAsync(
            deployment,
            client,
            cachedIdentity,
            progress => Report(context.Logger, progress),
            context.CancellationToken)
            .ConfigureAwait(false);

        await identityStore.SaveAsync(resource.Name, result.RemoteIdentity, context.CancellationToken).ConfigureAwait(false);
        string publicBaseUrl = ResolvePublicBaseUrl(deployment, result.PullZone);
        resource.TryGetBunnyStorageOutputs()?.Populate(result.StorageZone, result.PullZone, deployment.StorageEndpoint, publicBaseUrl);
    }

    internal static async Task<BunnyStorageCreateFlowResult> ExecuteAsync(
        BunnyStorageResolvedDeployment deployment,
        IBunnyStorageManagementClient client,
        BunnyStorageRemoteIdentityState? cachedIdentity,
        CancellationToken cancellationToken)
    {
        return await ExecuteCoreAsync(deployment, client, cachedIdentity, progressReporter: null, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<BunnyStorageCreateFlowResult> ExecuteCoreAsync(
        BunnyStorageResolvedDeployment deployment,
        IBunnyStorageManagementClient client,
        BunnyStorageRemoteIdentityState? cachedIdentity,
        Action<BunnyStorageDeploymentProgress>? progressReporter,
        CancellationToken cancellationToken)
    {
        progressReporter?.Invoke(BunnyStorageDeploymentDiagnostics.CreateProgress(
            "ResolvingConfiguration",
            $"Resolved Bunny Storage deployment configuration for storage zone '{deployment.StorageZoneName}'.",
            resourceName: null,
            deployment.StorageZoneName,
            providerStorageZoneId: null));

        BunnyStorageRemoteIdentityStateResult remoteIdentity = await new BunnyStorageRemoteIdentityResolver(client)
            .ResolveAsync(deployment.StorageZoneName, cachedIdentity, cancellationToken)
            .ConfigureAwait(false);

        BunnyStorageOwnershipResolutionResult ownership = BunnyStorageOwnershipResolver.Resolve(
            deployment.OwnershipMode,
            deployment.StorageZoneName,
            remoteIdentity.StorageZone);

        BunnyStorageCreateFlowResult result = await new BunnyStorageCreateFlow(client)
            .ExecuteAsync(deployment, ownership, cancellationToken)
            .ConfigureAwait(false);

        BunnyStorageZoneDetails reconciled = BunnyStorageReconciler.Reconcile(deployment, result.StorageZone);
        return result with { StorageZone = reconciled };
    }

    internal static string ResolvePublicBaseUrl(BunnyStorageResolvedDeployment deployment, BunnyPullZoneDetails? pullZone)
    {
        if (!string.IsNullOrWhiteSpace(deployment.Options.PublicBaseUrl))
        {
            return deployment.Options.PublicBaseUrl!;
        }

        if (pullZone is not null)
        {
            return $"https://{pullZone.Name}.b-cdn.net";
        }

        return $"{deployment.StorageEndpoint.TrimEnd('/')}/{Uri.EscapeDataString(deployment.StorageZoneName)}";
    }

    private static void Report(ILogger logger, BunnyStorageDeploymentProgress progress)
    {
        _deploymentProgress(logger, progress.Message, progress.ResourceName, progress.StorageZoneName, progress.ProviderStorageZoneId, null);
    }
}

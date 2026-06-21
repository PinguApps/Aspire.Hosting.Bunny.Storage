#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Bunny.Storage.Management;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting.Bunny.Storage;

internal static class BunnyStorageDeployTimeResolver
{
    public static Task<BunnyStorageResolvedDeployment> ResolveAsync(
        BunnyStorageDeploymentState state,
        IResource caller,
        PipelineStepContext context)
    {
        return ResolveAsync(state, caller, context.ExecutionContext, context.CancellationToken);
    }

    public static async Task<BunnyStorageResolvedDeployment> ResolveAsync(
        BunnyStorageDeploymentState state,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        string storageZoneName = await ResolveRequiredStringAsync(state.StorageZoneName, "storage zone name", caller, executionContext, cancellationToken).ConfigureAwait(false);
        string apiKey = await ResolveRequiredStringAsync(state.ApiKey, "API key", caller, executionContext, cancellationToken).ConfigureAwait(false);

        return new BunnyStorageResolvedDeployment(
            storageZoneName,
            state.OwnershipMode,
            new BunnyStorageManagementCredentials(apiKey),
            state.Options);
    }

    private static async Task<string> ResolveRequiredStringAsync(
        BunnyStorageValue value,
        string settingName,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        string resolvedValue = await ResolveStringAsync(value, caller, executionContext, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(resolvedValue)
            ? throw new InvalidOperationException($"Bunny Storage deployment requires a non-empty {settingName}.")
            : resolvedValue;
    }

    private static async Task<string> ResolveStringAsync(
        BunnyStorageValue value,
        IResource caller,
        DistributedApplicationExecutionContext? executionContext,
        CancellationToken cancellationToken)
    {
        if (value.LiteralValue is not null)
        {
            return value.LiteralValue;
        }

        ParameterResource parameter = value.Parameter
            ?? throw new InvalidOperationException("Bunny Storage value is not backed by a literal or parameter.");

        if (executionContext is null)
        {
            return await parameter.GetValueAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Bunny Storage parameter '{parameter.Name}' resolved to null.");
        }

        ValueProviderContext valueProviderContext = new()
        {
            ExecutionContext = executionContext,
            Caller = caller,
        };

        return await parameter.GetValueAsync(valueProviderContext, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Bunny Storage parameter '{parameter.Name}' resolved to null.");
    }
}

#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Pipelines;
using System.Reflection;

namespace Aspire.Hosting.Bunny.Storage;

public static class BunnyStorageBuilderExtensions
{
    private static readonly PropertyInfo _azureStorageBlobContainersProperty =
        typeof(AzureStorageResource).GetProperty("BlobContainers", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Aspire Azure Storage no longer exposes its blob-container provisioning collection.");

    [AspireExportIgnore(Reason = "C# callback overloads are not a stable guest-language transport contract.")]
    public static IResourceBuilder<AzureBlobStorageContainerResource> PublishToBunny(
        this IResourceBuilder<AzureBlobStorageContainerResource> builder,
        string storageZoneName,
        IResourceBuilder<ParameterResource> apiKey,
        BunnyStorageOwnershipMode ownershipMode = BunnyStorageOwnershipMode.CreateOrAdopt,
        Action<BunnyStorageDeploymentOptions>? configure = null)
    {
        return builder.PublishToBunny(BunnyStorageValue.FromString(storageZoneName), apiKey, ownershipMode, configure);
    }

    [AspireExportIgnore(Reason = "C# callback overloads are not a stable guest-language transport contract.")]
    public static IResourceBuilder<AzureBlobStorageContainerResource> PublishToBunny(
        this IResourceBuilder<AzureBlobStorageContainerResource> builder,
        BunnyStorageValue storageZoneName,
        IResourceBuilder<ParameterResource> apiKey,
        BunnyStorageOwnershipMode ownershipMode = BunnyStorageOwnershipMode.CreateOrAdopt,
        Action<BunnyStorageDeploymentOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(apiKey);
        return builder.PublishToBunny(storageZoneName, BunnyStorageValue.FromParameter(apiKey), ownershipMode, configure);
    }

    [AspireExportIgnore(Reason = "C# callback overloads are not a stable guest-language transport contract.")]
    public static IResourceBuilder<AzureBlobStorageContainerResource> PublishToBunny(
        this IResourceBuilder<AzureBlobStorageContainerResource> builder,
        BunnyStorageValue storageZoneName,
        BunnyStorageValue apiKey,
        BunnyStorageOwnershipMode ownershipMode = BunnyStorageOwnershipMode.CreateOrAdopt,
        Action<BunnyStorageDeploymentOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storageZoneName);
        ArgumentNullException.ThrowIfNull(apiKey);

        if (!Enum.IsDefined(ownershipMode))
        {
            throw new ArgumentOutOfRangeException(nameof(ownershipMode), ownershipMode, "The Bunny Storage ownership mode is not supported.");
        }

        BunnyStorageDeploymentOptions options = new();
        configure?.Invoke(options);
        options.Validate();

        RemoveExistingBunnyPipelineStep(builder.Resource);
        global::Aspire.Hosting.ResourceBuilderExtensions.ExcludeFromManifest(builder);
        DetachFromAzureProvisioning(builder.Resource);

        builder.WithAnnotation(
            new BunnyStorageDeploymentAnnotation(storageZoneName, apiKey, ownershipMode, options),
            ResourceAnnotationMutationBehavior.Replace);
        builder.WithAnnotation(
            new BunnyStorageOutputsAnnotation(new BunnyStorageOutputs(builder.Resource)),
            ResourceAnnotationMutationBehavior.Replace);

        AzureBlobStorageContainerResource resource = builder.Resource;
        return builder.WithPipelineStepFactory(
            $"bunny-storage-{builder.Resource.Name}",
            context => BunnyStorageDeploymentPipeline.ExecuteAsync(resource, context),
            dependsOn: [WellKnownPipelineSteps.DeployPrereq],
            requiredBy: [WellKnownPipelineSteps.Deploy],
            tags: [WellKnownPipelineTags.ProvisionInfrastructure],
            description: "Provision or reconcile the Bunny Storage zone.");
    }

    [AspireExportIgnore(Reason = "The options DTO includes callback-style C# configuration that is not ATS-compatible yet.")]
    public static IResourceBuilder<AzureBlobStorageContainerResource> PublishToBunnyForTypeScript(
        this IResourceBuilder<AzureBlobStorageContainerResource> builder,
        IResourceBuilder<ParameterResource> storageZoneName,
        IResourceBuilder<ParameterResource> apiKey,
        BunnyStorageDeploymentOptionsDto? options = null)
    {
        BunnyStorageDeploymentOptionsDto dto = options ?? new();
        return builder.PublishToBunny(
            BunnyStorageValue.FromParameter(storageZoneName),
            apiKey,
            dto.GetOwnershipMode(),
            target =>
            {
                BunnyStorageDeploymentOptions source = dto.ToDeploymentOptions();
                target.Region = source.Region;
                target.CreatePullZone = source.CreatePullZone;
                target.PullZoneName = source.PullZoneName;
                target.PublicBaseUrl = source.PublicBaseUrl;
                target.SetReplicationRegions([.. source.ReplicationRegions]);
            });
    }

    private static void RemoveExistingBunnyPipelineStep(AzureBlobStorageContainerResource resource)
    {
        for (int annotationIndex = 0; annotationIndex < resource.Annotations.Count; annotationIndex++)
        {
            if (resource.Annotations[annotationIndex] is not BunnyStorageDeploymentAnnotation)
            {
                continue;
            }

            int pipelineStepAnnotationIndex = annotationIndex + 1;
            while (pipelineStepAnnotationIndex < resource.Annotations.Count)
            {
                if (resource.Annotations[pipelineStepAnnotationIndex] is PipelineStepAnnotation)
                {
                    resource.Annotations.RemoveAt(pipelineStepAnnotationIndex);
                    break;
                }

                pipelineStepAnnotationIndex++;
            }

            return;
        }
    }

    private static void DetachFromAzureProvisioning(AzureBlobStorageContainerResource resource)
    {
        object? value = _azureStorageBlobContainersProperty.GetValue(resource.Parent.Parent);
        if (value is not ICollection<AzureBlobStorageContainerResource> blobContainers)
        {
            throw new InvalidOperationException("Aspire Azure Storage blob-container provisioning collection has an unexpected shape.");
        }

        blobContainers.Remove(resource);
    }
}

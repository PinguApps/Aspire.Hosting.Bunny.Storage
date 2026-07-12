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
    private static readonly PropertyInfo _azureStorageQueuesProperty =
        typeof(AzureStorageResource).GetProperty("Queues", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Aspire Azure Storage no longer exposes its queue provisioning collection.");
    private static readonly PropertyInfo _azureStorageDataLakeFileSystemsProperty =
        typeof(AzureStorageResource).GetProperty("DataLakeFileSystems", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Aspire Azure Storage no longer exposes its data-lake file-system provisioning collection.");
    private static readonly PropertyInfo _azureStorageQueueStorageBuilderProperty =
        typeof(AzureStorageResource).GetProperty("QueueStorageBuilder", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Aspire Azure Storage no longer exposes its queue service builder.");
    private static readonly PropertyInfo _azureStorageTableStorageBuilderProperty =
        typeof(AzureStorageResource).GetProperty("TableStorageBuilder", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Aspire Azure Storage no longer exposes its table service builder.");
    private static readonly PropertyInfo _azureStorageDataLakeStorageBuilderProperty =
        typeof(AzureStorageResource).GetProperty("DataLakeStorageBuilder", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Aspire Azure Storage no longer exposes its data-lake service builder.");

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
        if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            DetachFromAzureProvisioning(builder.Resource);
            ExcludeStorageParentFromManifestIfNoAzureStorageChildrenRemain(builder.ApplicationBuilder, builder.Resource);
            builder.WithPipelineConfiguration(_ => ThrowIfExcludedStorageParentHasAzureChildren(builder.ApplicationBuilder, builder.Resource));
        }

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

    [AspireExport("pinguapps.bunny.storage.publishToBunny", MethodName = "publishToBunny")]
    public static IResourceBuilder<AzureBlobStorageContainerResource> PublishToBunnyForTypeScript(
        this IResourceBuilder<AzureBlobStorageContainerResource> builder,
        IResourceBuilder<ParameterResource> storageZoneName,
        IResourceBuilder<ParameterResource> apiKey,
        BunnyStorageDeploymentOptionsDto? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storageZoneName);
        ArgumentNullException.ThrowIfNull(apiKey);

        BunnyStorageDeploymentOptionsDto dto = options ?? new();
        return builder.PublishToBunny(
            BunnyStorageValue.FromParameter(storageZoneName),
            apiKey,
            dto.GetOwnershipMode(),
            target => CopyOptions(dto.ToDeploymentOptions(), target));
    }

    private static void CopyOptions(BunnyStorageDeploymentOptions source, BunnyStorageDeploymentOptions target)
    {
        if (source.ExplicitSettings.Contains(nameof(BunnyStorageDeploymentOptions.Region)))
        {
            target.Region = source.Region;
        }

        if (source.ExplicitSettings.Contains(nameof(BunnyStorageDeploymentOptions.ReplicationRegions)))
        {
            target.SetReplicationRegions([.. source.ReplicationRegions]);
        }

        if (source.ExplicitSettings.Contains(nameof(BunnyStorageDeploymentOptions.CreatePullZone)))
        {
            target.CreatePullZone = source.CreatePullZone;
        }

        if (source.ExplicitSettings.Contains(nameof(BunnyStorageDeploymentOptions.PullZoneName)))
        {
            target.PullZoneName = source.PullZoneName;
        }

        if (source.ExplicitSettings.Contains(nameof(BunnyStorageDeploymentOptions.PublicBaseUrl)))
        {
            target.PublicBaseUrl = source.PublicBaseUrl;
        }
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

    internal static void DetachFromAzureProvisioning(AzureBlobStorageContainerResource resource)
    {
        GetAzureBlobContainers(resource.Parent.Parent).Remove(resource);
    }

    internal static void ExcludeStorageParentFromManifestIfNoAzureStorageChildrenRemain(
        IDistributedApplicationBuilder appBuilder,
        AzureBlobStorageContainerResource resource)
    {
        AzureStorageResource storage = resource.Parent.Parent;
        if (HasAzureStorageChildrenRequiringProvisioning(appBuilder, storage))
        {
            return;
        }

        if (storage.Annotations.Contains(ManifestPublishingCallbackAnnotation.Ignore))
        {
            return;
        }

        storage.Annotations.Add(ManifestPublishingCallbackAnnotation.Ignore);
    }

    internal static void ThrowIfExcludedStorageParentHasAzureChildren(
        IDistributedApplicationBuilder appBuilder,
        AzureBlobStorageContainerResource resource)
    {
        AzureStorageResource storage = resource.Parent.Parent;
        if (!storage.Annotations.Contains(ManifestPublishingCallbackAnnotation.Ignore)
            || !HasAzureStorageChildrenRequiringProvisioning(appBuilder, storage))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Azure Storage resource '{storage.Name}' was excluded because blob container '{resource.Name}' is published to Bunny Storage, " +
            "but additional Azure Storage children were added afterwards. Add Azure Storage queues, tables, data-lake resources, or direct blob-service references before calling PublishToBunny.");
    }

    internal static bool HasAzureStorageChildrenRequiringProvisioning(
        IDistributedApplicationBuilder appBuilder,
        AzureStorageResource resource)
    {
        return GetAzureBlobContainers(resource).Count > 0
            || GetProvisioningCollectionCount(_azureStorageQueuesProperty, resource) > 0
            || GetProvisioningCollectionCount(_azureStorageDataLakeFileSystemsProperty, resource) > 0
            || _azureStorageQueueStorageBuilderProperty.GetValue(resource) is not null
            || _azureStorageTableStorageBuilderProperty.GetValue(resource) is not null
            || _azureStorageDataLakeStorageBuilderProperty.GetValue(resource) is not null
            || appBuilder.Resources.OfType<AzureBlobStorageResource>().Any(blobStorage =>
                ReferenceEquals(blobStorage.Parent, resource)
                && IsResourceReferencedByAnotherResource(appBuilder, blobStorage))
            || appBuilder.Resources.Any(candidate => IsNonBlobAzureStorageDescendant(candidate, resource));
    }

    private static bool IsResourceReferencedByAnotherResource(
        IDistributedApplicationBuilder appBuilder,
        IResource resource)
    {
        return appBuilder.Resources
            .Where(candidate => !ReferenceEquals(candidate, resource))
            .SelectMany(candidate => candidate.Annotations)
            .Where(annotation => !string.Equals(annotation.GetType().Name, "WaitAnnotation", StringComparison.Ordinal))
            .Any(annotation => AnnotationReferencesResource(annotation, resource));
    }

    private static bool IsNonBlobAzureStorageDescendant(IResource candidate, AzureStorageResource storage)
    {
        if (candidate is AzureQueueStorageResource queueStorage)
        {
            return ReferenceEquals(queueStorage.Parent, storage);
        }

        if (candidate is AzureQueueStorageQueueResource queue)
        {
            return ReferenceEquals(queue.Parent.Parent, storage);
        }

        if (candidate is AzureTableStorageResource tableStorage)
        {
            return ReferenceEquals(tableStorage.Parent, storage);
        }

        if (candidate is AzureDataLakeStorageResource dataLakeStorage)
        {
            return ReferenceEquals(dataLakeStorage.Parent, storage);
        }

        if (candidate is AzureDataLakeStorageFileSystemResource fileSystem)
        {
            return ReferenceEquals(fileSystem.Parent.Parent, storage);
        }

        return false;
    }

    private static ICollection<AzureBlobStorageContainerResource> GetAzureBlobContainers(AzureStorageResource resource)
    {
        object? value = _azureStorageBlobContainersProperty.GetValue(resource);
        return value as ICollection<AzureBlobStorageContainerResource>
            ?? throw new InvalidOperationException("Aspire Azure Storage blob-container provisioning collection has an unexpected shape.");
    }

    private static int GetProvisioningCollectionCount(PropertyInfo property, AzureStorageResource resource)
    {
        object? value = property.GetValue(resource);
        return value is System.Collections.ICollection collection
            ? collection.Count
            : throw new InvalidOperationException($"Aspire Azure Storage collection '{property.Name}' has an unexpected shape.");
    }

    private static bool AnnotationReferencesResource(IResourceAnnotation annotation, IResource resource)
    {
        return annotation.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(property =>
            {
                object? value = property.GetValue(annotation);
                return ReferenceEquals(value, resource);
            });
    }
}

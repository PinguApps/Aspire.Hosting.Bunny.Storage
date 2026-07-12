using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting.Bunny.Storage;

public static class BunnyStorageReferenceBuilderExtensions
{
    [AspireExport("pinguapps.bunny.storage.withObjectStorage", MethodName = "withObjectStorage")]
    public static IResourceBuilder<ProjectResource> WithObjectStorageForTypeScript(
        this IResourceBuilder<ProjectResource> builder,
        IResourceBuilder<AzureBlobStorageContainerResource> storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        return builder.WithObjectStorage(storage);
    }

    [AspireExportIgnore(Reason = "Generic C# resource-builder callbacks are not a stable guest-language transport contract.")]
    public static IResourceBuilder<TDestination> WithObjectStorage<TDestination>(
        this IResourceBuilder<TDestination> builder,
        IResourceBuilder<AzureBlobStorageContainerResource> storage)
        where TDestination : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        AzureBlobStorageContainerResource resource = storage.Resource;
        AzureStorageResource storageResource = resource.Parent.Parent;
        bool storageParentWillBeExcluded = !builder.ApplicationBuilder.ExecutionContext.IsRunMode
            && !BunnyStorageBuilderExtensions.HasAzureStorageChildrenRequiringProvisioning(
                builder.ApplicationBuilder,
                storageResource);
        IResource[] waitResources = [];
        if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            waitResources = storageParentWillBeExcluded
                ? [resource, resource.Parent, storageResource]
                : [resource, resource.Parent];
        }
        RemoveAzureBlobReferenceAnnotations(
            builder,
            resource,
            waitResources);

        EnvironmentCallbackAnnotation? objectStorageEnvironmentCallback = null;
        objectStorageEnvironmentCallback = new EnvironmentCallbackAnnotation(context =>
        {
            string prefix = $"ObjectStorage__{resource.Name}__";
            if (context.ExecutionContext.Operation == DistributedApplicationOperation.Run)
            {
                context.EnvironmentVariables[$"{prefix}Provider"] = "AzureBlob";
                context.EnvironmentVariables[$"{prefix}Azure__ConnectionString"] = resource.ConnectionStringExpression;
                context.EnvironmentVariables[$"{prefix}Azure__ContainerName"] = resource.BlobContainerName;
                return;
            }

            ThrowIfAzureReferenceWasAddedAfterObjectStorage(builder.Resource, resource, objectStorageEnvironmentCallback!);
            RemoveAzureBlobConnectionReferences(context, resource);

            BunnyStorageOutputs outputs = resource.TryGetBunnyStorageOutputs()
                ?? throw new InvalidOperationException($"Blob container resource '{resource.Name}' has not been published to Bunny Storage.");
            context.EnvironmentVariables[$"{prefix}Provider"] = "Bunny";
            context.EnvironmentVariables[$"{prefix}PublicBaseUrl"] = outputs.PublicBaseUrl;
            context.EnvironmentVariables[$"{prefix}Bunny__StorageZoneName"] = outputs.StorageZoneName;
            context.EnvironmentVariables[$"{prefix}Bunny__AccessKey"] = outputs.AccessKey;
            context.EnvironmentVariables[$"{prefix}Bunny__Endpoint"] = outputs.StorageEndpoint;
        });
        builder.WithAnnotation(objectStorageEnvironmentCallback);
        builder.WithManifestPublishingCallback(_ =>
        {
            if (resource.TryGetBunnyStorageOutputs() is not null)
            {
                throw new InvalidOperationException(
                    "Bunny Storage object-storage references are produced during aspire deploy, not aspire publish. Use aspire deploy for Bunny-backed object storage, or run locally with Azurite.");
            }
        });

        return builder;
    }

    private static void ThrowIfAzureReferenceWasAddedAfterObjectStorage<TDestination>(
        TDestination targetResource,
        AzureBlobStorageContainerResource resource,
        EnvironmentCallbackAnnotation objectStorageEnvironmentCallback)
        where TDestination : IResourceWithEnvironment
    {
        int objectStorageCallbackIndex = targetResource.Annotations.IndexOf(objectStorageEnvironmentCallback);
        if (objectStorageCallbackIndex < 0)
        {
            return;
        }

        IResource[] waitResources = [resource, resource.Parent, resource.Parent.Parent];
        for (int i = objectStorageCallbackIndex + 1; i < targetResource.Annotations.Count; i++)
        {
            IResourceAnnotation annotation = targetResource.Annotations[i];
            bool isRelationship = annotation is ResourceRelationshipAnnotation relationship
                && ReferenceEquals(relationship.Resource, resource);
            bool isConnectionStringCallback = annotation is EnvironmentCallbackAnnotation environmentCallback
                && EnvironmentCallbackAddsConnectionStringReference(environmentCallback, targetResource, resource);
            bool isWaitAnnotation = waitResources.Any(waitResource =>
                AnnotationReferencesResource(annotation, waitResource, "WaitAnnotation"));

            if (isRelationship || isConnectionStringCallback || isWaitAnnotation)
            {
                throw new InvalidOperationException(
                    $"WithObjectStorage({resource.Name}) must be called after WithReference({resource.Name}) and WaitFor({resource.Name}). " +
                    "Bunny-backed object storage removes Azure Blob references during deploy, but Azure references were added afterwards.");
            }
        }
    }

    private static void RemoveAzureBlobReferenceAnnotations<TDestination>(
        IResourceBuilder<TDestination> builder,
        AzureBlobStorageContainerResource resource,
        IReadOnlyCollection<IResource> waitResourcesToRemove)
        where TDestination : IResourceWithEnvironment
    {
        bool hasAzureBlobRelationship = builder.Resource.Annotations.Any(annotation =>
            annotation is ResourceRelationshipAnnotation relationship
            && ReferenceEquals(relationship.Resource, resource));

        for (int i = builder.Resource.Annotations.Count - 1; i >= 0; i--)
        {
            IResourceAnnotation annotation = builder.Resource.Annotations[i];
            bool isRelationship = annotation is ResourceRelationshipAnnotation relationship
                && ReferenceEquals(relationship.Resource, resource);
            bool isConnectionStringCallback = hasAzureBlobRelationship
                && annotation is EnvironmentCallbackAnnotation environmentCallback
                && EnvironmentCallbackAddsConnectionStringReference(environmentCallback, builder.Resource, resource);
            bool isWaitAnnotation = waitResourcesToRemove.Any(waitResource =>
                AnnotationReferencesResource(annotation, waitResource, "WaitAnnotation"));

            if (isRelationship || isConnectionStringCallback || isWaitAnnotation)
            {
                builder.Resource.Annotations.RemoveAt(i);
            }
        }
    }

    private static bool AnnotationReferencesResource(
        IResourceAnnotation annotation,
        IResource resource,
        string annotationTypeName)
    {
        if (!string.Equals(annotation.GetType().Name, annotationTypeName, StringComparison.Ordinal))
        {
            return false;
        }

        object? value = annotation.GetType().GetProperty("Resource")?.GetValue(annotation);
        return ReferenceEquals(value, resource);
    }

    private static bool EnvironmentCallbackAddsConnectionStringReference(
        EnvironmentCallbackAnnotation annotation,
        IResourceWithEnvironment targetResource,
        AzureBlobStorageContainerResource referencedResource)
    {
        Dictionary<string, object> environmentVariables = [];
        EnvironmentCallbackContext context = new(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            targetResource,
            environmentVariables,
            CancellationToken.None);

        try
        {
            annotation.Callback(context).GetAwaiter().GetResult();
        }
        catch
        {
            return false;
        }

        return environmentVariables.TryGetValue($"ConnectionStrings__{referencedResource.Name}", out object? value)
            && value is ConnectionStringReference reference
            && ReferenceEquals(reference.Resource, referencedResource);
    }

    private static void RemoveAzureBlobConnectionReferences(
        EnvironmentCallbackContext context,
        AzureBlobStorageContainerResource resource)
    {
        foreach (KeyValuePair<string, object> environmentVariable in context.EnvironmentVariables.ToArray())
        {
            if (environmentVariable.Value is ConnectionStringReference reference
                && ReferenceEquals(reference.Resource, resource))
            {
                context.EnvironmentVariables.Remove(environmentVariable.Key);
            }
        }
    }
}

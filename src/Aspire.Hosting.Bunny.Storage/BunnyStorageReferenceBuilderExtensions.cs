using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting.Bunny.Storage;

public static class BunnyStorageReferenceBuilderExtensions
{
    [AspireExportIgnore(Reason = "Generic C# resource-builder callbacks are not a stable guest-language transport contract.")]
    public static IResourceBuilder<TDestination> WithObjectStorage<TDestination>(
        this IResourceBuilder<TDestination> builder,
        IResourceBuilder<AzureBlobStorageContainerResource> storage)
        where TDestination : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        AzureBlobStorageContainerResource resource = storage.Resource;
        builder.WithAnnotation(new EnvironmentCallbackAnnotation(context =>
        {
            string prefix = $"ObjectStorage__{resource.Name}__";
            if (context.ExecutionContext.Operation == DistributedApplicationOperation.Run)
            {
                context.EnvironmentVariables[$"{prefix}Provider"] = "AzureBlob";
                context.EnvironmentVariables[$"{prefix}Azure__ConnectionString"] = resource.ConnectionStringExpression;
                context.EnvironmentVariables[$"{prefix}Azure__ContainerName"] = resource.BlobContainerName;
                return;
            }

            BunnyStorageOutputs outputs = resource.TryGetBunnyStorageOutputs()
                ?? throw new InvalidOperationException($"Blob container resource '{resource.Name}' has not been published to Bunny Storage.");
            context.EnvironmentVariables[$"{prefix}Provider"] = "Bunny";
            context.EnvironmentVariables[$"{prefix}PublicBaseUrl"] = outputs.PublicBaseUrl;
            context.EnvironmentVariables[$"{prefix}Bunny__StorageZoneName"] = outputs.StorageZoneName;
            context.EnvironmentVariables[$"{prefix}Bunny__AccessKey"] = outputs.AccessKey;
            context.EnvironmentVariables[$"{prefix}Bunny__Endpoint"] = outputs.StorageEndpoint;
        }));

        return builder;
    }
}

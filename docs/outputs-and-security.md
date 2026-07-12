# Outputs And Security

## Runtime Reference

C#:

```csharp
builder.AddProject<Projects.Web>("web")
    .WithReference(media)
    .WithObjectStorage(media);
```

TypeScript project resource:

```ts
web = await web.withReference(media);
web = await web.withObjectStorage(media);
```

During local execution this produces Azure Blob settings. During deployment it removes the Azure connection reference and produces Bunny settings. The integration never fabricates an Azure Blob connection string for Bunny.

## Supplementary Outputs

| Output | Secret | Purpose |
| --- | --- | --- |
| `StorageZoneId` | No | Bunny provider ID. |
| `StorageZoneName` | No | Remote zone name. |
| `StorageEndpoint` | No | Region-specific Bunny Storage API endpoint. |
| `AccessKey` | Yes | Storage-zone password/access key used for runtime object operations. |
| `PullZoneId` | No | Pull Zone ID when one is available. |
| `PullZoneName` | No | Pull Zone name when one is available. |
| `PublicBaseUrl` | No | Public CDN/custom base URL used by `GetPublicUrl`. |

C# obtains them through `media.GetBunnyStorageOutputs()`. TypeScript uses `await media.getBunnyStorageOutputs()` and async property getters.

## Credential Boundary

Infrastructure-only:

- Bunny account API key supplied to `PublishToBunny`

Application-facing:

- storage-zone name
- region-specific storage endpoint
- storage-zone access key
- public base URL

The account API key can manage account resources and is never emitted as an application output. The storage-zone access key permits writes and must remain in server-side applications. Do not pass it to browser code or Blazor WebAssembly.

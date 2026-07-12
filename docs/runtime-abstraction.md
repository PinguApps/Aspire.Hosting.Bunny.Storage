# Runtime Abstraction

`PinguApps.Bunny.Storage` provides a small server-side abstraction over Azure Blob Storage and Bunny Storage:

```csharp
public interface IObjectStorage
{
    Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    string GetPublicUrl(string key);
}
```

Register stores from the `ObjectStorage` configuration emitted by the AppHost:

```csharp
builder.Services.AddObjectStorage(builder.Configuration);
```

When exactly one store exists, inject `IObjectStorage`. With multiple stores, inject `IObjectStorageProvider`:

```csharp
IObjectStorage media = provider.GetRequiredStorage("media");
```

Object keys are provider-neutral relative paths such as `uploads/avatar.png`. Absolute URLs and parent-directory segments are rejected. Store keys in application data and derive public URLs with `GetPublicUrl`.

The Azure implementation ensures the local container permits public blob reads when writing. The Bunny implementation uses the region-specific Storage API with the storage-zone access key and returns public URLs through the configured Pull Zone or custom base URL.

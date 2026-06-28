# Runtime Abstraction

App code depends on:

```csharp
IObjectStorage
IObjectStorageProvider
```

Do not reference Bunny API types from app code. Do not reference Azure Blob SDK types except inside the Azure implementation.

If one store is configured, `IObjectStorage` is registered directly. If multiple stores are configured, use:

```csharp
IObjectStorage storage = provider.GetRequiredStorage("media");
```

# Configuration

Runtime schema:

```text
ObjectStorage:{name}:Provider
ObjectStorage:{name}:PublicBaseUrl
ObjectStorage:{name}:Azure:ConnectionString
ObjectStorage:{name}:Azure:ContainerName
ObjectStorage:{name}:Bunny:StorageZoneName
ObjectStorage:{name}:Bunny:AccessKey
ObjectStorage:{name}:Bunny:Endpoint
```

Providers:

- `AzureBlob`
- `Bunny`

For non-default Bunny regions, use the region-specific storage endpoint, for example `https://ny.storage.bunnycdn.com`.

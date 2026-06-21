# Outputs And Security

Outputs:

- `StorageZoneId`
- `StorageZoneName`
- `StorageEndpoint`
- `AccessKey`
- `PullZoneId`
- `PullZoneName`
- `PublicBaseUrl`

`AccessKey` is secret. Other outputs are non-secret unless Bunny changes their semantics.

The Bunny account API key is used only by the AppHost deployment pipeline. The runtime Bunny access key is the storage zone password/access key. Never expose it to browser or Blazor WASM code.

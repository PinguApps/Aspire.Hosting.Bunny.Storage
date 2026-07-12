# Configuration

## Ownership Modes

| Mode | Missing zone | Existing compatible zone | Existing incompatible zone |
| --- | --- | --- | --- |
| `CreateOrAdopt` | Create | Adopt | Fail |
| `CreateOnly` | Create | Fail unless it is the cached verified identity from an earlier deployment | Fail |
| `ExistingOnly` | Fail | Adopt | Fail |

`CreateOrAdopt` is the usual starting point. Use `ExistingOnly` when another process owns the Bunny resource lifecycle.

## Deployment Options

| Setting | Default | Behaviour |
| --- | --- | --- |
| `Region` | `De` | Primary region. Supported: `De`, `Ny`, `La`, `Sg`. Existing zones must match. |
| `ReplicationRegions` | Empty | Set with `SetReplicationRegions(...)` in C# or an array in TypeScript. Existing zones must match exactly. `Syd` is supported only here. |
| `CreatePullZone` | `false` | Creates or adopts the configured Pull Zone when true. |
| `PullZoneName` | None | Required when `CreatePullZone` is true. |
| `PublicBaseUrl` | None | Required when no Pull Zone is created. When omitted with a Pull Zone, defaults to `https://<pull-zone>.b-cdn.net`. |

Region and replication settings are immutable desired state in v1. The integration validates them; it does not mutate an existing storage zone.

An adopted Pull Zone must:

- be enabled and not suspended
- have unsigned public delivery enabled
- be linked to the intended storage zone
- have the expected Bunny Storage origin when Bunny reports one

The package does not create custom Pull Zone hostnames or certificates. Configure those externally and set `PublicBaseUrl` to the resulting public origin.

## Runtime Schema

`WithObjectStorage` produces the following configuration:

```text
ObjectStorage:{name}:Provider
ObjectStorage:{name}:PublicBaseUrl
ObjectStorage:{name}:Azure:ConnectionString
ObjectStorage:{name}:Azure:ContainerName
ObjectStorage:{name}:Bunny:StorageZoneName
ObjectStorage:{name}:Bunny:AccessKey
ObjectStorage:{name}:Bunny:Endpoint
```

Supported provider values are `AzureBlob` and `Bunny`.

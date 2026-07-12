# Deployment Behaviour

`PublishToBunny` and `publishToBunny` are deploy-time integrations.

## Deploy Flow

During `aspire deploy`, the package:

1. Resolves the storage-zone name and Bunny account API key.
2. Lists storage zones and validates any cached provider identity.
3. Applies the selected ownership mode.
4. Creates the storage zone when allowed and required.
5. Validates zone name, primary region, and replication regions.
6. Creates or adopts the requested Pull Zone when enabled.
7. Validates Pull Zone state, security, origin, and storage-zone linkage.
8. Populates application-facing object-storage outputs.

The pipeline adds a step named `bunny-storage-<blob-container-resource-name>`. For `media`, `aspire deploy --non-interactive --list-steps` shows `bunny-storage-media` as infrastructure required by the final deploy step.

## Local Behaviour

`aspire start` keeps the Azure Blob container and Azurite configuration. Constructing or running the local AppHost does not call Bunny.

## Deploy Versus Publish

Bunny outputs exist only after the custom deployment step executes. `aspire publish` does not provision Bunny. Application resources using `WithObjectStorage` therefore reject manifest-only publishing and direct users to `aspire deploy`.

## Repeated Deployments

The explicit storage-zone name is the stable identity. Aspire deployment state also caches the Bunny provider ID. A repeated deployment verifies that the cached ID still exists and still has the configured name before reuse.

If a zone was deleted outside Aspire, deployment fails instead of silently adopting another resource. Remove the relevant state entries only when intentionally resetting ownership. The state prefix is:

```text
Aspire.Hosting.Bunny.Storage.RemoteIdentity.<blob-container-resource-name>
```

`aspire deploy --clear-cache` ignores cached identity for that run. Aspire does not persist replacement state during a clear-cache deployment, so remove stale entries for a permanent reset.

## Failure Behaviour

Deployment fails clearly when:

- required parameters are missing
- ownership mode conflicts with existing state
- cached remote identity is missing or renamed
- region or replication-region desired state differs
- Pull Zone creation lacks a name
- an adopted Pull Zone is disabled, suspended, secured, unlinked, or linked elsewhere
- Bunny does not return a storage-zone access key

Bunny may temporarily reserve a deleted storage-zone name. The integration retries the provider's `storagezone.name_taken` deletion response before surfacing an actionable failure.

The package never auto-deletes Bunny storage zones or Pull Zones.

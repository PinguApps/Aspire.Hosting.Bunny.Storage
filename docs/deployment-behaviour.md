# Deployment Behaviour

`PublishToBunny` attaches deployment metadata and a provisioning pipeline step to an `AzureBlobStorageContainerResource`.

During `dotnet run` / Aspire run, `WithObjectStorage` injects:

```text
ObjectStorage__media__Provider=AzureBlob
```

During deploy/publish, it injects:

```text
ObjectStorage__media__Provider=Bunny
```

No fake Azure Blob connection string is emitted for Bunny.

Ownership modes:

- `CreateOnly`: fail if the Bunny storage zone already exists.
- `ExistingOnly`: fail if it does not exist.
- `CreateOrAdopt`: create if missing, otherwise adopt.

Remote identity is persisted through Aspire deployment state to avoid silently adopting the wrong renamed resource.

If a Bunny storage zone or pull zone is deleted manually outside Aspire, the next deploy can fail because the
stored provider id no longer exists. This is intentional: the integration will not silently adopt or recreate a
different Bunny resource while stale deployment state is present.

To intentionally start fresh, remove the Bunny remote-identity entries from the Aspire deployment state file shown
by the deploy logs. Aspire stores section data as flattened keys prefixed by:

```text
Aspire.Hosting.Bunny.Storage.RemoteIdentity.{blob-container-resource-name}
```

For the sample `media` container, the section is:

```text
Aspire.Hosting.Bunny.Storage.RemoteIdentity.media
```

Remove entries whose keys start with:

```text
Aspire.Hosting.Bunny.Storage.RemoteIdentity.media:
```

Deleting the whole deployment state file also works, but it resets unrelated Azure deployment state too.

Running Aspire deploy with `--clear-cache` also ignores this cached Bunny identity. Because Aspire does not save
deployment state during a clear-cache run, use it for one-off recovery/testing; remove the stale Bunny entries if
you want the next normal deploy to persist and reuse the newly created Bunny identity.

Bunny can keep a deleted storage-zone name reserved for a short period while deletion completes. If Bunny returns
`storagezone.name_taken` with "currently being deleted", the integration treats that as transient and retries
briefly. If it still fails, wait for Bunny to release the name or deploy with a different storage-zone name.

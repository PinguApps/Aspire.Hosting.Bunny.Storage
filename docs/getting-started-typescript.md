# Getting started with TypeScript AppHost

The Bunny Storage integration exposes a TypeScript-friendly `PublishToBunnyForTypeScript` overload for AppHost package generation.

The supported runtime contract is the same as C# AppHosts:

- local runs use Azurite through Aspire Azure Blob Storage;
- deployed apps use Bunny Storage;
- application code consumes `IObjectStorage`;
- store object keys, not absolute URLs;
- keep Bunny write credentials server-side.

The repository does not currently include a TypeScript AppHost sample fixture.


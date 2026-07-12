# Samples And Validation

## Compile-Validated C# Snippet

[`samples/AppHostSnippets/BunnyStorageAppHostSnippets.cs`](../samples/AppHostSnippets/BunnyStorageAppHostSnippets.cs) is linked into the hosting test project, so public API drift fails the normal build.

## Manual C# Application

[`samples/BunnyStorageManual/`](../samples/BunnyStorageManual/) provides the runnable ASP.NET Core example. CI builds its AppHost and web project in Release configuration.

## TypeScript AppHost

[`samples/TypeScriptAppHost/`](../samples/TypeScriptAppHost/) hosts the same server application from an authored `apphost.mts`.

```powershell
npm install --no-audit --no-fund
aspire restore --non-interactive
npm run typecheck
aspire deploy --non-interactive --list-steps
```

Aspire generates `.aspire/modules`; do not edit or commit it.

CI uses [`eng/Validate-TypeScriptAppHostPackage.ps1`](../eng/Validate-TypeScriptAppHostPackage.ps1) and a separate fixture to pack the real NuGet package, restore it from an isolated local feed, type-check the authored TypeScript, and verify the deploy pipeline contains the Bunny step.

## Live Provider Test

The `Category=live-bunny` test creates disposable storage and Pull Zones, repeats deployment against the cached identity, exercises runtime put/read/exists/delete operations, and deletes the disposable provider resources in `finally` cleanup.

It requires `BUNNY_API_KEY` through the process environment or a repository-root `.env` file. Without credentials it skips cleanly.

```powershell
dotnet test tests/Aspire.Hosting.Bunny.Storage.Tests/Aspire.Hosting.Bunny.Storage.Tests.csproj `
  -c Release `
  --filter "Category=live-bunny"
```

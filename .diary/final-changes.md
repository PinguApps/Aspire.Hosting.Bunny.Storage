## Rolling state
- Goal: Prepare `PinguApps.Aspire.Hosting.Bunny.Storage` for a stable v1 release.
- Current plan: Implementation complete; review and commit the release-hardening changes.
- Open questions/risks: Newly created Bunny storage credentials briefly returned 401; the live readiness probe now retries bounded transient 401/404 responses.
- Next actions: Review the working tree, commit, and publish an RC before the stable tag.
- Key paths: `src/Aspire.Hosting.Bunny.Storage/`, `eng/Validate-TypeScriptAppHostPackage.ps1`, `samples/TypeScriptAppHost/`, `tests/Aspire.Hosting.Bunny.Storage.Tests/LiveBunnyStorageTests.cs`

## Session log
### 2026-07-12 15:40 +01:00 (feature/final-changes)
- Harden v1 package and TypeScript surface [api/build/tests/docs/release] (impact: high)
  - Why: Resolve package identity, TypeScript, sample-build, live-provider, and documentation launch blockers.
  - Change: Renamed hosting package, exported and package-gated TypeScript APIs, added TS sample/fixture, compile-checked samples, added cleanup-safe live coverage, updated Aspire/xUnit pins, and rewrote docs (cmds: `dotnet build`, `dotnet test`, `Validate-TypeScriptAppHostPackage.ps1`, `aspire start/wait/describe/stop`)
  - Notes: Release/sample builds are warning-free; 56 non-live tests pass; live test skips without `BUNNY_API_KEY`; no vulnerable or deprecated packages.

### 2026-07-12 19:50 +01:00 (feature/final-changes)
- Verify live Bunny lifecycle [tests/provider] (impact: high)
  - Why: Complete real-provider validation using credentials from the external test repository.
  - Change: Ran create/redeploy/Pull Zone/runtime operations/cleanup; added bounded readiness retry after an immediate post-create 401 (cmds: `dotnet test --filter Category=live-bunny`, Bunny list verification)
  - Notes: Live test passes; zero disposable storage zones or Pull Zones remain; 56 non-live tests still pass.

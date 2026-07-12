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

### 2026-07-12 19:57 +01:00 (feature/final-changes)
- Fix live readiness retry stream reuse [tests/provider] (impact: med)
  - Why: A transient 401 caused `StreamContent` disposal before the next retry reused the same stream.
  - Change: Create a fresh memory stream for every upload attempt; reran the credentialed live lifecycle test (files: `tests/Aspire.Hosting.Bunny.Storage.Tests/LiveBunnyStorageTests.cs`)
  - Notes: Live test passes after multiple retries; zero disposable storage zones or Pull Zones remain.

### 2026-07-12 20:29 +01:00 (feature/final-changes)
- Address PR #4 review threads [api/tests/docs/release] (impact: med)
  - Why: Resolve all six open review comments with independently traceable fixes.
  - Change: Added TS bridge guards, retained cleanup credentials, completed README sample, aligned build/pack versions, and required explicit live opt-in (cmds: `dotnet test`, `Validate-TypeScriptAppHostPackage.ps1`, live Bunny test)
  - Notes: 56 non-live and 1 live test pass; packed TS gate passes; live cleanup leaves zero disposable resources.

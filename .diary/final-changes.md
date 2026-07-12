## Rolling state
- Goal: Prepare `PinguApps.Aspire.Hosting.Bunny.Storage` for a stable v1 release.
- Current plan: Implementation complete; review and commit the release-hardening changes.
- Open questions/risks: Live Bunny test is implemented but skipped locally because this checkout has no root `.env` or `BUNNY_API_KEY`.
- Next actions: Supply `BUNNY_API_KEY` and run the `Category=live-bunny` test before tagging v1.
- Next actions: Review the working tree, commit, and publish an RC before the stable tag.
- Key paths: `src/Aspire.Hosting.Bunny.Storage/`, `eng/Validate-TypeScriptAppHostPackage.ps1`, `samples/TypeScriptAppHost/`, `tests/Aspire.Hosting.Bunny.Storage.Tests/LiveBunnyStorageTests.cs`

## Session log
### 2026-07-12 15:40 +01:00 (feature/final-changes)
- Harden v1 package and TypeScript surface [api/build/tests/docs/release] (impact: high)
  - Why: Resolve package identity, TypeScript, sample-build, live-provider, and documentation launch blockers.
  - Change: Renamed hosting package, exported and package-gated TypeScript APIs, added TS sample/fixture, compile-checked samples, added cleanup-safe live coverage, updated Aspire/xUnit pins, and rewrote docs (cmds: `dotnet build`, `dotnet test`, `Validate-TypeScriptAppHostPackage.ps1`, `aspire start/wait/describe/stop`)
  - Notes: Release/sample builds are warning-free; 56 non-live tests pass; live test skips without `BUNNY_API_KEY`; no vulnerable or deprecated packages.

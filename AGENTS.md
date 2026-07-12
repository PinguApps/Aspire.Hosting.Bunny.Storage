# AGENTS.md
## WORK DIARY

### Purpose
- Keep a small, high-signal diary so future sessions can resume quickly.

### When to read/write
- On session start: read the diary file (if it exists) to regain context.
- Once per response (just before replying): update the diary ONLY if you took meaningful actions (code/config changes, important commands run, decisions made, constraints/bugs discovered, tasks created that affect next steps). Otherwise: do not write.

### Location
- Always read/write inside `.diary/`

### Filename (from git branch)
- If branch is `vk/<suffix>` or `feature/<suffix>` → file is `.diary/<suffix>.md`
  - e.g. `vk/ab12-foo-bar` → `.diary/ab12-foo-bar.md`
- The branch should always have a prefix, but the prefix cannot be guaranteed, just use the suffix in every case after the `/`.

### Format (Markdown, compact)
- The file has two sections:

1) Rolling state (edit in place; keep ≤12 bullets total)
```
## Rolling state
- Goal: <one sentence>
- Current plan: <1–3 bullets>
- Open questions/risks: <0–3 bullets>
- Next actions: <1–5 bullets>
- Key paths: <optional; 1–5 entries>
```

2) Session log (append-only; per response keep ≤5 bullets)
```
## Session log
### <YYYY-MM-DD HH:MM Z> (<branch>)
- <Verb + object> [area] (impact: none|low|med|high)
  - Why: <reason/decision>
  - Change: <what changed> (files: <a,b,c> | cmds: `<...>`)
  - Notes: <gotchas/follow-ups> (optional)
```

### Compression rules
- Prefer deltas over narration (Add/Remove/Refactor/Fix).
- Use short tags for area: [ui] [api] [db] [auth] [infra] [build] [tests] etc...
- Include "Why" for any non-obvious decision and "Notes" for any caveat.
- Do not exceed caps; omit low-value detail.

## Operating Principles

### 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

### 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

### 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

## Repository-Specific Guidance

### Preserve Baseline Content
- Everything above this section is the user-authored baseline. Keep it intact.
- Keep this section concise and accurate for the current released state of the repository.

### Repository Overview
- This repository contains the `PinguApps.Aspire.Hosting.Bunny.Storage` and `PinguApps.ObjectStorage` NuGet packages. The hosting assembly and C# namespace remain `Aspire.Hosting.Bunny.Storage`.
- The hosting package lets an Aspire AppHost opt an `AzureBlobStorageContainerResource` into Bunny Storage during `aspire deploy`.
- Consumer usage starts from normal Aspire Azure Blob Storage, such as `builder.AddAzureStorage("storage").RunAsEmulator().AddBlobContainer("media", "media")`, then adds `.PublishToBunny(...)`.
- Local development should continue to use Azurite. Bunny behavior is deploy-only and opt-in.
- Application code should consume only `IObjectStorage` or `IObjectStorageProvider`.

### Current Product Contract
- This package is Bunny Storage plus optional Bunny Pull Zone CDN.
- Runtime provider selection is configuration-driven: local `AzureBlob`, deployed `Bunny`.
- Remote identity is the explicit Bunny storage-zone name.
- Supported ownership modes are `CreateOnly`, `ExistingOnly`, and `CreateOrAdopt`.
- Management authentication uses the Bunny account API key and is infrastructure-only.
- Runtime Bunny writes use the storage-zone access key/password and must stay server-side.
- Application-facing outputs expose object-storage settings, never the Bunny account API key.
- Store object keys in app data, not absolute URLs.
- Repeated deploys must target the same intended remote storage zone and only reconcile supported mutable settings.
- Deployment must fail clearly on unsafe drift or unreconcilable explicit settings.
- The package must not fake Azure Blob connection strings for Bunny.
- The package must not auto-delete remote Bunny resources.

### Key Paths
- `src/Aspire.Hosting.Bunny.Storage/` contains the Aspire hosting package source.
- `src/Aspire.Hosting.Bunny.Storage/Management/` contains the typed Bunny management client layer.
- `src/Aspire.Hosting.Bunny.Storage/Deployment/` contains deploy-time ownership, create, reconcile, identity, and diagnostics logic.
- `src/PinguApps.ObjectStorage/` contains the runtime provider-neutral object-storage abstraction and implementations.
- `tests/Aspire.Hosting.Bunny.Storage.Tests/` contains hosting package tests.
- `tests/PinguApps.ObjectStorage.Tests/` contains runtime package tests.
- `samples/AppHostSnippets/BunnyStorageAppHostSnippets.cs` is the compile-validated sample source used by docs tests.
- `samples/BunnyStorageManual/` contains the manual local/deploy sample.
- `samples/TypeScriptAppHost/` contains the maintained TypeScript AppHost sample.
- `eng/Validate-TypeScriptAppHostPackage.ps1` validates the packed NuGet TypeScript export surface and deploy-step discovery.
- `README.md` is the consumer-facing package guide and should stay aligned with shipped behavior.
- `.diary/` contains branch-specific session state and must be maintained per the diary rules above.

### Technical Baseline
- Target framework: `.NET 10`.
- Target Aspire version: currently pinned in `Directory.Packages.props`.
- Keep Aspire's built-in Azure Blob Storage container as the local resource of record.
- Preserve normal local Azurite behavior unless the work is explicitly about deploy-time Bunny behavior.
- Keep app-facing object-storage outputs separate from infrastructure-only management credentials.
- Bunny primary region and storage-zone identity are create-time or fail-fast checks.
- Pull zones must be linked to the intended storage zone before publishing public URLs.

### Testing And Docs
- Any behavior change must update or add active coverage in the relevant test project.
- Live-provider scenarios must stay opt-in, skip cleanly without Bunny secrets, and leave the remote account unchanged after the run.
- TypeScript support must be validated from the packed NuGet package, not only through local project references.
- Keep `README.md`, `AGENTS.md`, samples, and tests in sync with the actual shipped behavior.

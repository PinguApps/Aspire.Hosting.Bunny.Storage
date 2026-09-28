## Rolling state
- Goal: Mirror Upstash Redis's weekly and manual release-draft publisher for Bunny Storage.
- Current plan: PR #10 is implemented; CI and PR Agent passed on the latest workflow commit.
- Open questions/risks: Gitar automatic review is paused for the billing period, so its exact-HEAD check is absent; `GITHUB_TOKEN` release events do not trigger the publish workflow.
- Next actions: Await Gitar review when automatic processing resumes or a manual review is authorized; merge PR #10 after its current-HEAD review passes.
- Key paths: `.github/workflows/publish-release-draft.yml`, `.github/workflows/publish.yml`

## Session log
### 2026-09-28 14:21 +01:00 (agent/weekly-publish-draft)
- Add weekly release-draft publisher [build] (impact: med)
  - Why: Match Upstash Redis commit `dafedf4`: publish an existing draft only when `main` advances beyond the last published release.
  - Change: Added Sunday 09:17 UTC/manual workflow and reusable package publishing with safe tag handoff (files: `.github/workflows/publish-release-draft.yml`, `.github/workflows/publish.yml`).
  - Notes: Kept the existing Blacksmith runner and NuGet OIDC permission; `actionlint` passed with the known custom runner label ignored, and `git diff --check` passed.
- Fix vulnerable SourceLink pin [build] (impact: low)
  - Why: PR #10 restore and TypeScript gates failed on `Microsoft.Build.Tasks.Git` 10.0.300 (NU1902).
  - Change: Bumped `Microsoft.SourceLink.GitHub` to patched 10.0.303 (file: `Directory.Packages.props`).
  - Notes: Restore, 65 tests, and packed TypeScript validation passed locally; one live test skipped.
- Address release-workflow review [build] (impact: med)
  - Why: Gitar found a swallowed API failure; Copilot found non-main dispatch and package-version handoff risks.
  - Change: Added explicit Bash, main-only job guard, and runner environment version use (files: `.github/workflows/publish-release-draft.yml`, `.github/workflows/publish.yml`).
  - Notes: Each review thread has its own commit; `actionlint` passed with the existing custom runner label ignored.
- Handle empty release list [build] (impact: low)
  - Why: PR Agent found `jq add` returns `null` for zero releases.
  - Change: Defaulted release aggregation to `[]` (file: `.github/workflows/publish-release-draft.yml`).
  - Notes: `actionlint`, 65 tests, packed TypeScript gate, and PR Agent review passed; Gitar dashboard approved but automatic current-HEAD review paused.

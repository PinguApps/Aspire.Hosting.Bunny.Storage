## Rolling state
- Goal: Mirror Upstash Redis's weekly and manual release-draft publisher for Bunny Storage.
- Current plan: Complete CI and review on PR #10.
- Open questions/risks: A release published with `GITHUB_TOKEN` does not trigger the release event workflow, so the scheduled workflow calls package publishing directly.
- Next actions: Push SourceLink CI fix, await checks and current-HEAD review, complete final audit.
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

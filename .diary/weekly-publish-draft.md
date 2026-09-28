## Rolling state
- Goal: Mirror Upstash Redis's weekly and manual release-draft publisher for Bunny Storage.
- Current plan: Push CI and review fixes, then audit PR #10.
- Open questions/risks: Gitar automatic review is paused for the billing period; `GITHUB_TOKEN` release events do not trigger the publish workflow.
- Next actions: Push fixes, reply to review threads, await checks and current-HEAD review, complete final audit.
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

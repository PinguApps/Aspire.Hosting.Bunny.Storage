## Rolling state
- Goal: Mirror Upstash Redis's weekly and manual release-draft publisher for Bunny Storage.
- Current plan: Open PR and complete CI and review.
- Open questions/risks: A release published with `GITHUB_TOKEN` does not trigger the release event workflow, so the scheduled workflow calls package publishing directly.
- Next actions: Push implementation, open PR, complete review audit.
- Key paths: `.github/workflows/publish-release-draft.yml`, `.github/workflows/publish.yml`

## Session log
### 2026-09-28 14:21 +01:00 (agent/weekly-publish-draft)
- Add weekly release-draft publisher [build] (impact: med)
  - Why: Match Upstash Redis commit `dafedf4`: publish an existing draft only when `main` advances beyond the last published release.
  - Change: Added Sunday 09:17 UTC/manual workflow and reusable package publishing with safe tag handoff (files: `.github/workflows/publish-release-draft.yml`, `.github/workflows/publish.yml`).
  - Notes: Kept the existing Blacksmith runner and NuGet OIDC permission; `actionlint` passed with the known custom runner label ignored, and `git diff --check` passed.

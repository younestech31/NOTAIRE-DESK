# Development workflow

- `main` is the integration baseline; do not push directly.
- Work on feature branches and submit pull requests.
- CI runs on each PR and pushes to setup/**, feature/**, fix/** and chore/** branches.
- Every run publishes Windows x64 and web-preview artifacts.

## Download builds
GitHub → Actions → latest **NOTAIRE DESK CI** run → Artifacts:
- `notaire-desk-windows-x64`
- `notaire-desk-web-preview`

The initial Windows package is framework-dependent; install .NET 10 Desktop Runtime on the test PC.

## Vercel preview
Create a Vercel project with Root Directory `preview`. Configure repository Actions secrets `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` and repository Actions variable `VERCEL_PREVIEW_ENABLED=true`. PR deployment is skipped until that variable is enabled. Preview is synthetic-data-only; never add office API URLs, credentials or real documents.

## Local prerequisites
Windows 10/11 x64 for WPF, .NET 10 SDK, Node.js 22+ for preview. PostgreSQL deployment is added after schema and migrations are designed.

Do not merge a PR until explicitly approved by the repository owner.

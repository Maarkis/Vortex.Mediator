# Contributing

Contributions are welcome.

## Scope

You can contribute with:

- bug fixes
- tests
- documentation
- performance improvements
- source generator improvements
- API review proposals

## Development Setup

Requirements:

- .NET SDK 10
- semantic commits following Conventional Commits

Build:

```bash
dotnet build Vortex.sln -c Release -m:1 -v minimal
```

Optional native Git hook:

```bash
./scripts/setup-hooks.sh
```

On Windows PowerShell:

```powershell
./scripts/setup-hooks.ps1
```

With that enabled, `pre-commit` runs:

```bash
dotnet format Vortex.sln --verify-no-changes
dotnet build Vortex.sln
```

Run tests:

```bash
dotnet test tests/Vortex.Mediator.Tests/Vortex.Mediator.Tests.csproj -c Release -v minimal
```

Create local packages:

```bash
dotnet pack src/Vortex.Mediator.Abstractions/Vortex.Mediator.Abstractions.csproj -c Release -o artifacts/packages
dotnet pack src/Vortex.Mediator/Vortex.Mediator.csproj -c Release -o artifacts/packages
```

## Guidelines

- keep public API changes intentional and reviewed
- prefer compile-time solutions over runtime reflection in hot paths
- add or update unit tests for behavior changes
- keep naming in PascalCase
- keep test style aligned with the current NUnit suite
- keep changes scoped and atomic when possible

## Pull Requests

Suggested flow:

1. create a branch
2. implement the change
3. add or update tests
4. run build and test locally
5. open the pull request with a clear description

Contributors with merged pull requests are added automatically to [CONTRIBUTORS.md](./CONTRIBUTORS.md) through an automated pull request with auto-merge enabled.

## Commit Style

This repository uses semantic commits when possible, for example:

- `feat: add instance convention handlers`
- `fix: avoid duplicate notification dispatch`
- `docs: add package usage guide`
- `test: cover stream edge cases`

Breaking changes must use the Conventional Commits `!` marker or a `BREAKING CHANGE:` footer.

## Release Flow

- pull requests must use a Conventional Commits title such as `feat: add async pipeline support`
- `ci.yml` validates format, build and tests on pull requests and pushes to `main`
- `release.yml` runs on merges to `main` and uses Release Please to open or update the release PR
- when Release Please creates a GitHub release, the same workflow calls `publish-nuget.yml`
- `publish-nuget.yml` packs `Vortex.Mediator.Abstractions` and `Vortex.Mediator` and pushes them to NuGet
- `publish-nuget.yml` also supports manual `dry-run` execution to validate package generation without publishing

Required GitHub secret:

- `NUGET_API_KEY`: API key with permission to publish both NuGet packages

Optional GitHub secret:

- `RELEASE_PLEASE_TOKEN`: personal access token if you want GitHub Actions to run on Release Please PRs and tags created by the bot

Required GitHub repository settings:

- GitHub Actions workflow permissions must be set to `Read and write`
- GitHub Actions must be allowed to create pull requests
- repository auto-merge must be enabled if you want contributor update PRs to merge automatically

Manual publish validation:

1. open GitHub Actions
2. run `Publish NuGet`
3. provide `release-tag` and `package-version`
4. keep `dry-run = true` to upload `.nupkg` files as workflow artifacts instead of publishing to NuGet

## Licensing

By contributing to this repository, you agree that your contributions will be licensed under the [MIT License](./LICENSE).

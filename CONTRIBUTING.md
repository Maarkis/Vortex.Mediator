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

## Commit Style

This repository uses semantic commits when possible, for example:

- `feat: add instance convention handlers`
- `fix: avoid duplicate notification dispatch`
- `docs: add package usage guide`
- `test: cover stream edge cases`

## Licensing

By contributing to this repository, you agree that your contributions will be licensed under the [MIT License](./LICENSE).

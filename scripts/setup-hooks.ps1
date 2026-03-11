$ErrorActionPreference = "Stop"

$repoRoot = (git rev-parse --show-toplevel).Trim()

git -C $repoRoot config core.hooksPath .githooks

Write-Host "Configured Git hooks path to .githooks"

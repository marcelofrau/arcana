#!/usr/bin/env bash
# Clean all build artifacts (Linux/macOS)
set -euo pipefail
REPO_ROOT="$(dirname "$0")/.."

dotnet clean "$REPO_ROOT/src/Arcana.slnx" -c Debug -v q --nologo 2>/dev/null || true

find "$REPO_ROOT" -type d \( -name bin -o -name obj -o -name dist \) -exec rm -rf {} + 2>/dev/null || true

echo "Clean complete"

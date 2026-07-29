#!/usr/bin/env bash
# Run Arcana.App in development mode (Linux/macOS)
set -euo pipefail
dotnet run --project "$(dirname "$0")/../src/Arcana.App"

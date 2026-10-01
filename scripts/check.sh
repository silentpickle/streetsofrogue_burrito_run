#!/bin/bash
set -euo pipefail
project_dir="$(cd "$(dirname "$0")/.." && pwd)"
cd "$project_dir"
export DOTNET_CLI_HOME="$project_dir/.tools/dotnet-home"
export NUGET_PACKAGES="$project_dir/.tools/nuget"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

case "${1:-}" in
    '') verify=(--verify-no-changes) ;;
    --fix) verify=() ;;
    *) echo 'Usage: bash scripts/check.sh [--fix]' >&2; exit 2 ;;
esac
dotnet restore BurritoRun.sln --nologo -m:1 /nodeReuse:false
if [ "${1:-}" = '--fix' ]; then
    # Only safe cleanup: do not auto-delete reflection-discovered members.
    dotnet format whitespace BurritoRun.sln --no-restore
    dotnet format style BurritoRun.sln --no-restore --diagnostics IDE0005
else
    dotnet build BurritoRun.sln -c Release --no-restore --nologo -m:1 /nodeReuse:false /p:UseSharedCompilation=false
    dotnet format BurritoRun.sln --no-restore "${verify[@]}"
fi

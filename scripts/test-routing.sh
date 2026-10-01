#!/bin/bash
set -euo pipefail
project_dir="$(cd "$(dirname "$0")/.." && pwd)"
cd "$project_dir"
export DOTNET_CLI_HOME="$project_dir/.tools/dotnet-home"
export NUGET_PACKAGES="$project_dir/.tools/nuget"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet run --project tests/DeliveryRoute.Tests/DeliveryRoute.Tests.csproj /p:UseSharedCompilation=false

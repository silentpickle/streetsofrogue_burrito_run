#!/bin/bash
set -euo pipefail
project_dir="$(cd "$(dirname "$0")/.." && pwd)"
cd "$project_dir"
export DOTNET_CLI_HOME="$project_dir/.tools/dotnet-home"
export NUGET_PACKAGES="$project_dir/.tools/nuget"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet build src/BurritoRun.Profile/BurritoRun.Profile.csproj -c Release --nologo /p:UseSharedCompilation=false "$@"
dotnet build src/BurritoRun/BurritoRun.csproj -c Release --nologo /p:UseSharedCompilation=false "$@"
mkdir -p .tools/loader/BepInEx/plugins .tools/loader/BepInEx/patchers
cp src/BurritoRun/bin/Release/net472/BurritoRun.dll .tools/loader/BepInEx/plugins/BurritoRun.dll
cp src/BurritoRun.Profile/bin/Release/net472/BurritoRun.Profile.dll .tools/loader/BepInEx/patchers/BurritoRun.Profile.dll

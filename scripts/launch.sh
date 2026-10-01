#!/bin/bash
set -euo pipefail
project_dir="$(cd "$(dirname "$0")/.." && pwd)"
game_app="${SOR_GAME_APP:-/Users/davidcrandall/Library/Application Support/Steam/steamapps/common/Streets of Rogue/StreetsOfRogueMac.app}"
loader_dir="$project_dir/.tools/loader"
if pgrep -f '/StreetsOfRogueMac.app/Contents/MacOS/Streets of Rogue' >/dev/null; then
    echo 'Close the running Streets of Rogue first, then launch this again.'
    exit 1
fi
if [ ! -f "$loader_dir/BepInEx/plugins/BurritoRun.dll" ]; then
    echo 'Prototype is not built yet. Run: bash scripts/build.sh'
    exit 1
fi
export BURRITO_PROFILE_PATH="$project_dir/.runtime/profile"
mkdir -p "$BURRITO_PROFILE_PATH/CloudData" "$BURRITO_PROFILE_PATH/BackupData" "$project_dir/.runtime/logs"
if [ ! -f "$BURRITO_PROFILE_PATH/.initialized" ]; then
    original_profile='/Users/davidcrandall/Library/Application Support/com.Streets-of-Rogue.Streets-of-Rogue/CloudData'
    for name in GameUnlocks.dat GameSettings.dat; do
        if [ -f "$original_profile/$name" ]; then
            cp -p "$original_profile/$name" "$BURRITO_PROFILE_PATH/CloudData/$name"
        fi
    done
    touch "$BURRITO_PROFILE_PATH/.initialized"
fi
export SteamAppId=512900
export SteamGameId=512900
stamp="$(date '+%Y%m%d-%H%M%S')-$$"
log="$project_dir/.runtime/logs/game-$stamp.log"
echo 'Burrito Run: launching with a separate prototype save profile.'
echo "Session log: $log"
echo 'Quick Start a single-player run. Order at any computer.'
cd "$loader_dir"
# Doorstop's relative dylib resolution requires this working directory on macOS.
set +e
bash run_bepinex.sh "$game_app" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile "$log" "$@" >"$project_dir/.runtime/logs/console-$stamp.log" 2>&1
result=$?
set -e
cp "$loader_dir/BepInEx/LogOutput.log" "$project_dir/.runtime/logs/plugin-$stamp.log" 2>/dev/null || true
if [ "$result" -ne 0 ]; then
    echo "Game exited with code $result. Logs are saved in .runtime/logs."
fi
exit "$result"

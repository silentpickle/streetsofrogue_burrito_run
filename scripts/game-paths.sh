#!/bin/bash
# Read shared installation defaults without evaluating paths as shell code.
# Sourcing exports them for launchers; executing prints them for setup diagnostics.
game_paths_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
game_paths_resolved="$(python3 - "$game_paths_root/GamePaths.props" <<'PY'
import os
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

values = dict(os.environ)
for prop in ET.parse(sys.argv[1]).getroot().find('PropertyGroup'):
    expected = "'$(" + prop.tag + ")' == ''"
    if prop.attrib != {'Condition': expected}:
        sys.exit('GamePaths.props supports only ordered defaults with an empty-value condition')
    if not values.get(prop.tag):
        try:
            values[prop.tag] = re.sub(r'\$\(([^)]+)\)', lambda m: values[m[1]], prop.text or '')
        except KeyError as error:
            sys.exit('Undefined game path property: ' + str(error))
    value = values[prop.tag]
    if '\n' in value or '\r' in value or not Path(value).is_absolute():
        sys.exit(prop.tag + ' must be an absolute path without line breaks')
    print(prop.tag + '=' + value)
PY
)" || return 1 2>/dev/null || exit 1
while IFS= read -r game_paths_setting; do
    export "$game_paths_setting"
done <<< "$game_paths_resolved"
if [ "${BASH_SOURCE[0]}" = "$0" ]; then
    printf '%s\n' "$game_paths_resolved"
fi
unset game_paths_root game_paths_resolved game_paths_setting

#!/usr/bin/env python3
"""Create an allowlisted source snapshot; never include runtime or Git history."""

import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import uuid
import zipfile

from source_package_policy import audit_archive


def main():
    """Create a unique allowlisted source archive and a hash manifest.

    Read back the actual ZIP to verify its bytes while keeping runtime data, dependencies
    and Git history local.
    """
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--audit', type=Path, help='Audit an existing ZIP instead of packaging')
    parser.add_argument('--game-dir', type=Path, help='Also reject exact copies of locally installed game files')
    args = parser.parse_args()
    if args.audit is not None:
        print(json.dumps(audit_archive(args.audit, args.game_dir), indent=2))
        return
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    output = root / '.runtime' / 'publication' / f'{stamp}-{uuid.uuid4().hex[:8]}'
    output.mkdir(parents=True, exist_ok=False)
    files = set()
    for path in root.iterdir():
        if path.is_file() and (path.suffix in {'.md', '.sln', '.props', '.command'}
                               or path.name in {'LICENSE', 'Makefile', '.gitignore',
                                                '.editorconfig', 'projectbrief.txt'}):
            files.add(path)
    allowed = {
        'src': {'.cs', '.csproj'},
        'tests': {'.cs', '.csproj', '.py'},
        'tools': {'.cs', '.csproj'},
        'scripts': {'.sh', '.py'},
        'docs': {'.md', '.json', '.png', '.jpg', '.jpeg', '.webp', '.svg'},
    }
    for directory, suffixes in allowed.items():
        for path in (root / directory).rglob('*'):
            parts = path.relative_to(root).parts
            if any(part in {'bin', 'obj', '__pycache__'} or part.startswith('.')
                   for part in parts):
                continue
            if path.is_file() and path.suffix in suffixes:
                files.add(path)
    manifest = []
    archive = output / 'burrito-run-source.zip'
    pending = archive.with_suffix('.zip.pending')
    with zipfile.ZipFile(pending, 'x', compression=zipfile.ZIP_DEFLATED) as bundle:
        for path in sorted(files):
            if path.is_symlink() or root not in path.resolve().parents:
                raise ValueError(f'Refusing symlink/outside source: {path}')
            relative = path.relative_to(root).as_posix()
            data = path.read_bytes()
            digest = hashlib.sha256(data).hexdigest()
            info = zipfile.ZipInfo.from_file(path, 'burrito-run/' + relative)
            info.compress_type = zipfile.ZIP_DEFLATED
            bundle.writestr(info, data)
            manifest.append({'path': relative, 'bytes': len(data), 'sha256': digest})
    # Read the actual archive back and verify every selected file byte-for-byte.
    with zipfile.ZipFile(pending) as bundle:
        if len(bundle.namelist()) != len(manifest):
            raise ValueError('Archive member count differs from the selected files')
        for item in manifest:
            data = bundle.read('burrito-run/' + item['path'])
            if hashlib.sha256(data).hexdigest() != item['sha256']:
                raise ValueError('Archive bytes differ from selected source: ' + item['path'])
    audit = audit_archive(pending, args.game_dir)
    pending.rename(archive)
    (output / 'content-audit.json').write_text(json.dumps(audit, indent=2) + '\n')
    receipt = {'format': 1, 'createdUtc': stamp, 'archive': archive.name,
               'archiveSha256': hashlib.sha256(archive.read_bytes()).hexdigest(),
               'fileCount': len(manifest), 'files': manifest}
    (output / 'manifest.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(f'{archive}\n{len(manifest)} files; archive readback hashes verified')


if __name__ == '__main__':
    main()

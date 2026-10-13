"""Reject game/dependency payloads from the developer source archive."""

import hashlib
from pathlib import Path, PurePosixPath
import stat
import zipfile


# These are reviewed documentation captures/data, not extracted game assets.
# Changing a file requires explicitly reviewing it and updating its hash here.
REVIEWED_FILES = {
    'docs/images/damaged-window-observation.png':
        'f5a89fd277da48a38a7753b01eb9b965ae08e76f1df39db310b874641f908e3e',
    'docs/images/native-npc-fixture.png':
        '518b2909b8ad698f9cdab671abfde08ac3aee89c3389ef8cfafcce46af3adda2',
    'docs/images/world-decision-inspector.png':
        '5bf7804dd7fea1bf306370ebeb36f4c97afb5f4903ed101f71b4e8d1864c501e',
    'docs/examples/npc-choice-v2.json':
        '25ff5978b708d32395985afbdbd0c4ab37755c1e440cae8d228730cb52803eec',
}
ROOT_NAMES = {'LICENSE', 'Makefile', '.gitignore', '.editorconfig', 'projectbrief.txt'}
SOURCE_SUFFIXES = {
    'src': {'.cs', '.csproj'},
    'tests': {'.cs', '.csproj', '.py'},
    'tools': {'.cs', '.csproj'},
    'scripts': {'.sh', '.py'},
    'docs': {'.md'},
}
FORBIDDEN_PARTS = {
    '.git', '.tools', '.runtime', 'bin', 'obj', '__pycache__', 'managed',
    'clouddata', 'backupdata', 'streamingassets', 'resources', 'libraries',
}
BINARY_PREFIXES = (
    b'MZ', b'\x7fELF', b'UnityFS', b'UnityRaw', b'UnityWeb', b'PK\x03\x04',
    b'\xfe\xed\xfa\xce', b'\xce\xfa\xed\xfe', b'\xfe\xed\xfa\xcf',
    b'\xcf\xfa\xed\xfe', b'\xca\xfe\xba\xbe', b'\xbe\xba\xfe\xca',
)


def validate_member(name, data):
    """Accept only source text or an exact reviewed documentation file.

    Checking bytes as well as names prevents a renamed DLL or newly copied asset
    from passing merely because it has a source/documentation extension.
    """
    path = PurePosixPath(name)
    if ('\\' in name or path.is_absolute() or '..' in path.parts or
            not path.parts or path.parts[0] != 'burrito-run' or
            path.as_posix() != name or len(path.parts) < 2):
        raise ValueError('Invalid archive path: ' + name)
    relative = PurePosixPath(*path.parts[1:])
    if any(part.lower() in FORBIDDEN_PARTS for part in relative.parts):
        raise ValueError('Forbidden game/runtime/dependency path: ' + name)
    if data.startswith(BINARY_PREFIXES):
        raise ValueError('Executable, archive or Unity payload: ' + name)
    reviewed = REVIEWED_FILES.get(relative.as_posix())
    if reviewed is not None:
        if hashlib.sha256(data).hexdigest() != reviewed:
            raise ValueError('Reviewed documentation file changed: ' + name)
        return
    if len(relative.parts) == 1:
        allowed = (relative.name in ROOT_NAMES or
                   relative.suffix in {'.md', '.sln', '.props', '.command'})
    else:
        allowed = relative.suffix in SOURCE_SUFFIXES.get(relative.parts[0], set())
        if any(part.startswith('.') for part in relative.parts):
            allowed = False
    if not allowed:
        raise ValueError('File is outside the source allowlist: ' + name)
    try:
        data.decode('utf-8')
    except UnicodeDecodeError as error:
        raise ValueError('Non-text payload disguised as source: ' + name) from error
    if b'\0' in data:
        raise ValueError('Binary NUL in source text: ' + name)


def installed_file_hashes(game_directory):
    """Hash the local game installation for an optional exact-copy comparison.

    The comparison detects whole files, even if renamed, but cannot certify the
    origin or rights of arbitrary source fragments or documentation screenshots.
    """
    root = Path(game_directory)
    if not root.is_dir():
        raise ValueError('Game directory does not exist')
    hashes = {}
    count = total = 0
    for path in root.rglob('*'):
        if path.is_file() and not path.is_symlink():
            data = path.read_bytes()
            hashes.setdefault(hashlib.sha256(data).hexdigest(), []).append(
                path.relative_to(root).as_posix())
            count += 1
            total += len(data)
    if not count:
        raise ValueError('Game directory contains no files to compare')
    return hashes, count, total


def audit_archive(archive, game_directory=None):
    """Audit the actual ZIP, optionally comparing every member with local game files.

    Rejecting invalid members before publication keeps runtime data and game code
    outside this source release; the result does not certify Workshop installation.
    """
    game_hashes, game_count, game_bytes = {}, 0, 0
    if game_directory is not None:
        game_hashes, game_count, game_bytes = installed_file_hashes(game_directory)
    seen = set()
    with zipfile.ZipFile(archive) as bundle:
        if not bundle.infolist():
            raise ValueError('Empty source archive')
        for member in bundle.infolist():
            if member.filename in seen:
                raise ValueError('Duplicate archive member: ' + member.filename)
            seen.add(member.filename)
            if member.is_dir() or stat.S_ISLNK(member.external_attr >> 16):
                raise ValueError('Directory or symlink archive member: ' + member.filename)
            data = bundle.read(member)  # Also verifies this member's CRC.
            validate_member(member.filename, data)
            if hashlib.sha256(data).hexdigest() in game_hashes:
                raise ValueError('Exact installed-game file copy: ' + member.filename)
    return {
        'status': 'PASS', 'entriesChecked': len(seen),
        'gameFilesCompared': game_count, 'gameBytesHashed': game_bytes,
        'exactGameFileMatches': [],
        'limits': 'Source-content audit; not copyright clearance or Workshop compatibility.',
    }

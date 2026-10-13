"""Negative controls for accidental game/dependency inclusion in source releases."""

import importlib.util
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
import zipfile


ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location(
    'source_package_policy', ROOT / 'scripts/source_package_policy.py')
POLICY = importlib.util.module_from_spec(spec)
spec.loader.exec_module(POLICY)


class SourcePackagePolicyTests(unittest.TestCase):
    def test_plain_project_source_is_accepted(self):
        POLICY.validate_member('burrito-run/src/Example.cs', b'namespace BurritoRun {}\n')

    def test_game_dll_extension_is_rejected(self):
        with self.assertRaisesRegex(ValueError, 'allowlist'):
            POLICY.validate_member('burrito-run/src/Assembly-CSharp.dll', b'fake')

    def test_renamed_executable_is_rejected(self):
        with self.assertRaisesRegex(ValueError, 'payload'):
            POLICY.validate_member('burrito-run/src/Example.cs', b'MZbinary')

    def test_unity_bundle_is_rejected_even_under_reviewed_image_name(self):
        name = next(iter(POLICY.REVIEWED_FILES))
        with self.assertRaisesRegex(ValueError, 'Unity payload'):
            POLICY.validate_member('burrito-run/' + name, b'UnityFSbundle')

    def test_unreviewed_image_is_rejected(self):
        with self.assertRaisesRegex(ValueError, 'allowlist'):
            POLICY.validate_member('burrito-run/docs/images/copied-texture.png', b'\x89PNG')

    def test_changed_reviewed_file_is_rejected(self):
        name = next(iter(POLICY.REVIEWED_FILES))
        with self.assertRaisesRegex(ValueError, 'changed'):
            POLICY.validate_member('burrito-run/' + name, b'different image')

    def test_binary_disguised_as_text_is_rejected(self):
        for data in (b'\xff\xfe', b'valid\0text'):
            with self.subTest(data=data), self.assertRaises(ValueError):
                POLICY.validate_member('burrito-run/docs/readme.md', data)

    def test_traversal_and_runtime_paths_are_rejected(self):
        for name in ('burrito-run/../game.cs', '/burrito-run/src/a.cs',
                     'burrito-run\\src\\a.cs', 'burrito-run/.runtime/save.md',
                     'burrito-run/src/Managed/Agent.cs'):
            with self.subTest(name=name), self.assertRaises(ValueError):
                POLICY.validate_member(name, b'text')

    def test_duplicate_zip_members_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            archive = Path(directory) / 'source.zip'
            with zipfile.ZipFile(archive, 'w') as bundle:
                bundle.writestr('burrito-run/README.md', 'first')
                with self.assertWarns(UserWarning):
                    bundle.writestr('burrito-run/README.md', 'second')
            with self.assertRaisesRegex(ValueError, 'Duplicate'):
                POLICY.audit_archive(archive)

    def test_renamed_exact_game_file_is_rejected_by_hash(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            game = root / 'game'
            game.mkdir()
            data = b'synthetic native code copied into a source-named file'
            (game / 'native-file.dat').write_bytes(data)
            archive = root / 'source.zip'
            with zipfile.ZipFile(archive, 'w') as bundle:
                bundle.writestr('burrito-run/src/LooksLikeSource.cs', data)
            with self.assertRaisesRegex(ValueError, 'Exact installed-game file copy'):
                POLICY.audit_archive(archive, game)

    def test_failed_pack_keeps_pending_evidence_without_final_zip(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'scripts').mkdir()
            (root / 'docs').mkdir()
            for name in ('package-source.py', 'source_package_policy.py'):
                shutil.copy2(ROOT / 'scripts' / name, root / 'scripts' / name)
            (root / 'docs/unreviewed.png').write_bytes(b'\x89PNG')
            result = subprocess.run([sys.executable, str(root / 'scripts/package-source.py')],
                                    capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(list(root.rglob('burrito-run-source.zip')), [])
            self.assertEqual(len(list(root.rglob('*.zip.pending'))), 1)

    def test_real_reviewed_files_match_their_pinned_hashes(self):
        for name in POLICY.REVIEWED_FILES:
            with self.subTest(name=name):
                POLICY.validate_member('burrito-run/' + name, (ROOT / name).read_bytes())


if __name__ == '__main__':
    unittest.main()

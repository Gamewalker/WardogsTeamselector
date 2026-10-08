"""Check that release notes cover exactly the changes since the prior release."""
import os
import json
import hashlib
from pathlib import Path
import subprocess
import tempfile
import unittest

from release import release_notes, write_update_manifest


class ReleaseNotesTests(unittest.TestCase):
    def test_manifest_matches_both_published_binaries(self):
        with tempfile.TemporaryDirectory() as directory:
            assets = [Path(directory) / f"WardogsTeamselector-win-x64-{variant}.exe"
                      for variant in ("with-runtime", "without-runtime")]
            for index, asset in enumerate(assets):
                asset.write_bytes(b"MZ" + bytes([index]))
            manifest = json.loads(write_update_manifest(assets, "build-23-abcdef1").read_text(encoding="utf-8"))
            self.assertEqual(manifest["tag_name"], "build-23-abcdef1")
            self.assertFalse(manifest["draft"] or manifest["prerelease"])
            for asset, entry in zip(assets, manifest["assets"]):
                self.assertEqual(entry["name"], asset.name)
                self.assertEqual(entry["size"], asset.stat().st_size)
                self.assertEqual(entry["digest"], "sha256:" + hashlib.sha256(asset.read_bytes()).hexdigest())

    def test_moving_legacy_notes_does_not_repeat_them(self):
        previous_directory = Path.cwd()
        with tempfile.TemporaryDirectory() as directory:
            try:
                os.chdir(directory)
                subprocess.run(['git', 'init', '-q'], check=True)
                subprocess.run(['git', 'config', 'user.name', 'Release test'], check=True)
                subprocess.run(['git', 'config', 'user.email', 'test@example.com'], check=True)
                legacy = Path('.github/release-notes/old.md')
                legacy.parent.mkdir(parents=True)
                legacy.write_text('Earlier release explanation.', encoding='utf-8')
                subprocess.run(['git', 'add', '.'], check=True)
                subprocess.run(['git', 'commit', '-qm', 'Earlier release'], check=True)
                self.assertIn('Earlier release explanation.', release_notes(None, 'HEAD'))
                Path('docs/releases').mkdir(parents=True)
                legacy.rename('docs/releases/old.md')
                Path('docs/releases/new.md').write_text('New release explanation.', encoding='utf-8')
                subprocess.run(['git', 'add', '-A'], check=True)
                subprocess.run(['git', 'commit', '-qm', 'Move documentation'], check=True)
                incremental = release_notes('HEAD~1', 'HEAD')
                self.assertNotIn('Earlier release explanation.', incremental)
                self.assertIn('New release explanation.', incremental)
                self.assertIn('Earlier release explanation.', release_notes(None, 'HEAD'))
            finally:
                os.chdir(previous_directory)

    def test_initial_and_incremental_notes(self):
        previous_directory = Path.cwd()
        with tempfile.TemporaryDirectory() as directory:
            try:
                os.chdir(directory)
                subprocess.run(['git', 'init', '-q'], check=True)
                subprocess.run(['git', 'config', 'user.name', 'Release test'], check=True)
                subprocess.run(['git', 'config', 'user.email', 'test@example.com'], check=True)
                notes = Path('docs/releases')
                notes.mkdir(parents=True)
                (notes / 'first.md').write_text('### Erste Änderung\n\n- Teamklicks verbessert.', encoding='utf-8')
                subprocess.run(['git', 'add', '.'], check=True)
                subprocess.run(['git', 'commit', '-qm', 'Improve team clicks'], check=True)
                base = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
                initial = release_notes(None, base)
                self.assertIn('Teamklicks verbessert.', initial)
                self.assertIn('Improve team clicks', initial)
                self.assertIn('WardogsTeamselector-win-x64-with-runtime.exe', initial)
                self.assertIn('WardogsTeamselector-win-x64-without-runtime.exe', initial)
                self.assertIn('.NET 10 Desktop Runtime (x64)', initial)
                (notes / 'second.md').write_text('### Neue Änderung\n\n- Releases automatisch erstellen.', encoding='utf-8')
                subprocess.run(['git', 'add', '.'], check=True)
                subprocess.run(['git', 'commit', '-qm', 'Publish releases automatically'], check=True)
                incremental = release_notes(base, 'HEAD')
                self.assertIn('Releases automatisch erstellen.', incremental)
                self.assertIn('Publish releases automatically', incremental)
                self.assertNotIn('Teamklicks verbessert.', incremental)
                self.assertNotIn('Improve team clicks', incremental)
                Path('other.txt').write_text('updated')
                subprocess.run(['git', 'add', '.'], check=True)
                subprocess.run(['git', 'commit', '-qm', 'Fix runner configuration'], check=True)
                fallback = release_notes('HEAD~1', 'HEAD')
                self.assertIn('Fix runner configuration', fallback)
                self.assertNotIn('Releases automatisch erstellen.', fallback)
            finally:
                os.chdir(previous_directory)


if __name__ == '__main__':
    unittest.main()

"""Check that release notes cover exactly the changes since the prior release."""
import os
from pathlib import Path
import subprocess
import tempfile
import unittest

from release import release_notes


class ReleaseNotesTests(unittest.TestCase):
    def test_initial_and_incremental_notes(self):
        previous_directory = Path.cwd()
        with tempfile.TemporaryDirectory() as directory:
            try:
                os.chdir(directory)
                subprocess.run(['git', 'init', '-q'], check=True)
                subprocess.run(['git', 'config', 'user.name', 'Release test'], check=True)
                subprocess.run(['git', 'config', 'user.email', 'test@example.com'], check=True)
                notes = Path('.github/release-notes')
                notes.mkdir(parents=True)
                (notes / 'first.md').write_text('### Erste Änderung\n\n- Teamklicks verbessert.', encoding='utf-8')
                subprocess.run(['git', 'add', '.'], check=True)
                subprocess.run(['git', 'commit', '-qm', 'Improve team clicks'], check=True)
                base = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
                initial = release_notes(None, base)
                self.assertIn('Teamklicks verbessert.', initial)
                self.assertIn('Improve team clicks', initial)
                self.assertIn('WardogsTeamselector.exe', initial)
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

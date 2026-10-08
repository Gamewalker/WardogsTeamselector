"""Publish both EXE variants with notes since the last published release."""
import json
import os
from pathlib import Path
import subprocess
import tempfile


def command(*args):
    return subprocess.check_output(args, text=True).strip()


def release_notes(base, head):
    revision = f"{base}..{head}" if base else head
    commits = command("git", "log", "--reverse", "--format=- %s (%h)", revision)
    if base:
        files = command("git", "diff", "--name-only", "--find-renames", "--diff-filter=AM", base, head,
                        "--", "docs/releases/*.md", ".github/release-notes/*.md")
    else:
        files = command("git", "ls-tree", "-r", "--name-only", head,
                        "--", "docs/releases/", ".github/release-notes/")
    explanations = [command("git", "show", f"{head}:{name}")
                    for name in files.splitlines() if name.endswith(".md")]
    body = "## Änderungen\n\n" + ("\n\n".join(explanations) if explanations else
                                    "Die Änderungen dieser Version sind unten mit ihren Commit-Titeln aufgeführt.")
    body += "\n\n## Enthaltene Commits\n\n" + (commits or "Keine zusätzlichen Commits.")
    body += ("\n\n## Download\n\n"
             "- `WardogsTeamselector-win-x64-with-runtime.exe`: portable Windows-x64-Anwendung mit gebündelter .NET-Runtime; keine Runtime-Installation erforderlich.\n"
             "- `WardogsTeamselector-win-x64-without-runtime.exe`: kleinere Windows-x64-Anwendung ohne gebündelte Runtime; benötigt die installierte **.NET 10 Desktop Runtime (x64)**.\n"
             "\nBeide Varianten enthalten dieselben Funktionen und verwenden dieselben Einstellungen.\n")
    return body


def main():
    head = os.environ["GITHUB_SHA"]
    tag = f"build-{os.environ['GITHUB_RUN_NUMBER']}-{head[:7]}"
    assets = [Path(f"dist/WardogsTeamselector-win-x64-{variant}.exe")
              for variant in ("with-runtime", "without-runtime")]
    for asset in assets:
        if not asset.is_file():
            raise RuntimeError(f"Published EXE missing: {asset}")
    releases = json.loads(command("gh", "release", "list", "--limit", "100",
                                  "--exclude-drafts", "--exclude-pre-releases",
                                  "--json", "tagName"))
    if any(r["tagName"] == tag for r in releases):
        # Retries repair a missing asset without creating another release.
        command("gh", "release", "upload", tag, *map(str, assets), "--clobber")
        return
    base = None
    for release in releases:
        candidate = release["tagName"]
        if subprocess.run(["git", "merge-base", "--is-ancestor", candidate, head],
                          stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode == 0:
            base = candidate
            break
    with tempfile.TemporaryDirectory() as directory:
        notes = Path(directory) / "release-notes.md"
        notes.write_text(release_notes(base, head), encoding="utf-8")
        command("gh", "release", "create", tag, *map(str, assets), "--target", head,
                "--title", f"WardogsTeamselector – Build {os.environ['GITHUB_RUN_NUMBER']}",
                "--notes-file", str(notes), "--latest")


if __name__ == "__main__":
    main()

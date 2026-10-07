"""Publish the built EXE with notes for changes since the last published release."""
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
        files = command("git", "diff", "--name-only", "--diff-filter=AM", base, head,
                        "--", ".github/release-notes/*.md")
    else:
        files = command("git", "ls-tree", "-r", "--name-only", head,
                        "--", ".github/release-notes/")
    explanations = [command("git", "show", f"{head}:{name}")
                    for name in files.splitlines() if name.endswith(".md")]
    body = "## Änderungen\n\n" + ("\n\n".join(explanations) if explanations else
                                    "Die Änderungen dieser Version sind unten mit ihren Commit-Titeln aufgeführt.")
    body += "\n\n## Enthaltene Commits\n\n" + (commits or "Keine zusätzlichen Commits.")
    body += "\n\n## Download\n\n`WardogsTeamselector.exe` ist die portable Windows-x64-Anwendung mit gebündelter .NET-Runtime.\n"
    return body


def main():
    head = os.environ["GITHUB_SHA"]
    tag = f"build-{os.environ['GITHUB_RUN_NUMBER']}-{head[:7]}"
    asset = Path("dist/WardogsTeamselector.exe")
    if not asset.is_file():
        raise RuntimeError("Published EXE missing")
    releases = json.loads(command("gh", "release", "list", "--limit", "100",
                                  "--exclude-drafts", "--exclude-pre-releases",
                                  "--json", "tagName"))
    if any(r["tagName"] == tag for r in releases):
        # Retries repair a missing asset without creating another release.
        command("gh", "release", "upload", tag, str(asset), "--clobber")
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
        command("gh", "release", "create", tag, str(asset), "--target", head,
                "--title", f"WardogsTeamselector – Build {os.environ['GITHUB_RUN_NUMBER']}",
                "--notes-file", str(notes), "--latest")


if __name__ == "__main__":
    main()

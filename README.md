# 🐺 WardogsTeamselector

Choose your Wardogs team with one keypress. This open-source Windows tool repeatedly clicks your selected team and stops when it detects that you have joined the game.

## 📥 Download

**[Get the latest release →](https://github.com/Gamewalker/WardogsTeamselector/releases/latest)**

Under **Assets**, download a portable EXE and run it. No installation of the tool is required.

- **With runtime:** `WardogsTeamselector-win-x64-with-runtime.exe` includes the runtime. Choose this if you are unsure.
- **Without runtime:** `WardogsTeamselector-win-x64-without-runtime.exe` is a smaller download and requires the **[.NET 10 Desktop Runtime (x64)](https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe)**.

Both variants have the same features and share your settings. You need **Windows x64** and Wardogs. Keep the EXE in a writable folder so it can install updates.

## 🎬 English video tour

[![English tour of the current WardogsTeamselector interface](docs/media/wardogs-teamselector-demo-en.gif)](https://github.com/Gamewalker/WardogsTeamselector/raw/refs/heads/main/docs/media/wardogs-teamselector-demo-en.mp4)

**[Watch or download the English video](https://github.com/Gamewalker/WardogsTeamselector/raw/refs/heads/main/docs/media/wardogs-teamselector-demo-en.mp4)** · 48 seconds · English interface and captions · no audio

[English captions (SRT)](docs/media/wardogs-teamselector-demo-en.srt)

The tour uses current app screenshots, embedded game reference images and a labeled waiting-state fixture in test mode. It demonstrates the interface, not a live game session or a successful join.

## 🚀 Quick start

1. **Open Wardogs** and go to the team selection screen.
2. In **Setup**, choose **Find game / load image**. Check that the colored team outlines and click points match the three team cards. Adjust the monitor, game area or team regions if necessary.
3. Choose **Save & open Operation** to finish setup.
4. Try **test mode** first: activate a team with its button or F-key and watch the status. New profiles start in test mode and send no mouse input.
5. When the setup is correct, open **Diagnostics**, turn off **Use test mode · no mouse input**, and save your settings. Changing the mode stops the current attempt, so activate your team again afterward.

By default, activating a team brings the matching game window to the foreground. You can disable this under **Configuration → Game focus**; then switch to the game yourself. If Windows prevents automatic focus, the attempt waits for you to bring the game to the foreground.

## ⌨️ Team controls

| Default key | Action |
| --- | --- |
| **F6** | 🔵 Activate Blue |
| **F7** | 🔴 Activate Red |
| **F8** | 🟢 Activate Green |
| **ESC** | ⏹️ Stop the attempt immediately |

You can activate a team before the selection screen appears. The tool waits for the game window, game focus and a stable selection dialog before the first click. After that, it keeps trying the same team even if dialog detection flickers or the dialog disappears. It stops successfully only when the five white HUD bars at the bottom right are detected continuously for at least **0.5 seconds** across at least three detections.

During an attempt, the active team button becomes **Stop**. Click it or press **ESC** to cancel. The other two buttons appear dimmed but remain available to switch teams. All three return to normal after a stop or successful join detection. Loss of game focus, a changed or missing game area, and capture or input errors also stop an attempt; activate a team again to retry.

In **Configuration**, choose three distinct **F1–F24** shortcuts and adjust the randomized click interval. The default is **50–70 ms**; supported values are **50–60,000 ms**, with the maximum at least as large as the minimum.

## ✨ Current features

- **Five task areas:** Setup and Operation sit on the left of the tab navigation; Configuration, Diagnostics and Group management sit on the right. Setup connects and calibrates the game, Operation selects a team, Configuration adjusts behavior and detection, Diagnostics contains tests and logs, and Group management handles invitations and membership approval.
- **Windows 11 style:** a dark native WPF interface, clear team colors, keyboard focus indicators and a high-resolution app icon. The window title shows the current release build.
- **20 offline interface languages:** English is the first-launch default. Change the language in the header without restarting, interrupting an attempt or losing unsaved edits. The choice is remembered separately. Available languages are English, German, Spanish, Italian, Portuguese, Polish, Dutch, French, Turkish, Russian, Ukrainian, Arabic, Hindi, Bengali, Indonesian, Vietnamese, Thai, Simplified Chinese, Japanese and Korean. Arabic text uses right-to-left direction. Native Windows dialogs follow your Windows language; translations may still need refinement.
- **Groups and private administration:** create multiple groups, invite and approve members, and use one shared active group selection across operation and administration. Copy private admin access to another instance, or take it over exclusively to revoke previous admin access. Online groups require a separately deployed Cloudflare Free service; see [the group guide](docs/groups.md).
- **Saved profiles and clear edit state:** save or discard changes in Setup, Configuration and Diagnostics. Operation keeps its focus on team controls. Activating a team uses valid current settings but does not automatically save them. Valid saved profiles reopen in Operation; new or invalid profiles open in Setup.
- **Calibration and preview controls:** automatic game-window detection, selectable monitors, manual game bounds and editable team regions. Extra preview captures run only in Setup and Diagnostics and can be disabled without disabling the automation's required detection.
- **Diagnostics and project links:** check embedded dialog and HUD reference images, inspect detection scores and logs, and export a screenshot or diagnostic report. **About the app** includes project, license and bug-report links. The bug-report button opens a prepared GitHub issue for you to review and submit; attachments are added manually.

Settings are stored per Windows user in `%LOCALAPPDATA%\WardogsTeamselector\`, including `settings.json`, `language.json` and `updates.json`.

## 🔄 Automatic updates

Published release EXEs check for updates at startup and every **six hours**. The app reads the public release's `update.json` and downloads the matching runtime variant without consuming the anonymous GitHub API quota. Older releases without a manifest use an API fallback. Downloads are checked for size, Windows EXE format and SHA-256 checksum.

When a newer version is available, an **Update** button appears in the header beside **About the app**. It is disabled while the download is running. After verification, click it to install and restart; a failed download can be retried there. You can also use **Install update and restart** under **Configuration → Automatic updates**, or let the prepared update install when you close the app. Unsaved changes are handled by the normal save prompt.

Your settings and runtime variant are preserved. The previous executable is kept beside the EXE as `<EXE>.previous`. Configuration also provides a manual update check and an option to disable automatic downloads. Local development builds and GUI smoke runs do not update automatically. If an old release is already blocked by API rate limits, download the latest release manually once. [More about updates (German)](docs/updates.md)

## 📚 Help and troubleshooting

Keep the game's selection screen unobstructed; a second monitor is useful. With a different aspect ratio, HDR or unusual UI scaling, check the preview and [calibration guide](docs/calibration.md) before enabling real clicks. For non-16:9 geometry, verify the game area and team regions and confirm the geometry in Setup.

If the tool keeps waiting, check the window-title and process filters, selected monitor and game focus. If it stops unexpectedly, inspect the status reason and diagnostic log. Reference checks validate detection only and stop an active attempt. Real game capture and mouse input depend on your desktop and game setup; the reference-image tour does not verify that integration.

The detailed guides below are currently **in German**:

- [Usage guide](docs/usage.md)
- [Game area and team calibration](docs/calibration.md)
- [Troubleshooting and diagnostics](docs/troubleshooting.md)
- [Documentation index and release notes](docs/README.md)

[Report a problem on GitHub](https://github.com/Gamewalker/WardogsTeamselector/issues) with the steps to reproduce, build number, runtime variant, status reason and, if possible, a diagnostic export. You can also start a report from About the app or Diagnostics.

## 🛠️ Build from source

On Windows x64 with the **.NET 10 SDK**, run:

```powershell
.\build.ps1 -Tests
```

The script checks detection, HUD recognition, automation, configuration, localization, updates and Windows platform behavior, then writes both portable EXE variants to `dist/`. It uses `.tools/dotnet` if available, otherwise the installed `dotnet`. GUI and real-game checks require an interactive Windows desktop; automated fixtures do not replace testing capture and input in Wardogs.

GitHub Actions builds both variants and publishes a release after a successful push to `main`. See [development](docs/development.md) and [testing](docs/TESTING.md) for details (German). The English video's [source and regeneration instructions](docs/media/demo-source/README.md) are also included.

## 📜 License

WardogsTeamselector is licensed under **GNU GPL v3.0**. See [LICENSE](LICENSE) for the full text and the [license guide (German)](docs/license.md) for more information.

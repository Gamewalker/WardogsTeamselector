# English video source

The 48-second tour uses the current application's English WPF views with English captions. It has no audio. The MP4 is H.264, 1600 × 1080, 30 fps, with streaming metadata at the beginning; the looping GIF provides the GitHub README preview. An English SRT file contains the same captions for accessibility.

## Regenerate

On an interactive Windows desktop with the .NET 10 SDK and FFmpeg (including libx264), run from the repository root:

```powershell
.\docs\media\demo-source\CreateDemo.ps1
```

If the tools are not on PATH, supply their executable paths:

```powershell
.\docs\media\demo-source\CreateDemo.ps1 -Dotnet C:\tools\dotnet\dotnet.exe -Ffmpeg C:\tools\ffmpeg\ffmpeg.exe
```

The script compiles the current app source into a separate documentation capture executable, captures the UI, composes eight six-second scenes and replaces the English MP4, GIF and SRT in `docs/media/`. Temporary captures and encoded scenes are written to the ignored `artifacts/english-demo/` directory. The capture harness calls private UI methods via reflection; if those methods change, update `Program.cs` accordingly.

## What the tour demonstrates

- Actual English Setup, Operation, Configuration, Diagnostics and About views.
- A **UI-only waiting fixture** showing Blue as Stop and the other team buttons dimmed. The automation controller remains stopped.
- Embedded dialog and HUD reference images. These are static test images, not a live game recording or proof of a successful join.
- An explanatory update slide. Update downloads, installation and restarts are disabled in capture mode.

The harness runs with global hotkeys disabled, uses default settings in memory, sends no mouse input and does not save the user's profile or language selection. As in the app's existing smoke harness, construction still loads the user's profile; an existing valid legacy profile may be migrated by `SettingsStore.Load`.

Before committing regenerated media, inspect each scene for clipped text, verify the active-team controls and English labels, and decode the complete MP4 and GIF with FFmpeg. Keep the README's duration and demo description aligned with the video.

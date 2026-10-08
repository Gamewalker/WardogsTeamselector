param(
    [string]$Dotnet = 'dotnet',
    [string]$Ffmpeg = 'ffmpeg'
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$work = Join-Path $root 'artifacts/english-demo'
$captures = Join-Path $work 'captures'
New-Item -ItemType Directory -Force $work | Out-Null
& $Dotnet run --project (Join-Path $PSScriptRoot 'CaptureDemo.csproj') -c Release -- $captures
if ($LASTEXITCODE -ne 0) { throw 'English UI capture failed.' }

$scenes = @(
    @{ Image='operation'; Title='WardogsTeamselector'; Detail='Choose Blue, Red or Green with F6, F7 or F8. Start in test mode.'; Note='CURRENT ENGLISH INTERFACE / NO MOUSE INPUT' },
    @{ Image='setup'; Title='01 / Connect and calibrate'; Detail='Find the game, check the colored team outlines, then save your setup.'; Note='EMBEDDED REFERENCE IMAGE / NOT A LIVE GAME' },
    @{ Image='waiting-fixture'; Title='02 / Activate, switch or stop'; Detail='The active team becomes Stop. Other teams stay selectable. ESC cancels.'; Note='WAITING-STATE UI FIXTURE / CONTROLLER STOPPED' },
    @{ Image='configuration'; Title='03 / Make it yours'; Detail='Adjust click intervals and hotkeys. Auto focus brings the game forward.'; Note='DEFAULT INTERVAL: 50-70 MS / HOTKEYS: F1-F24' },
    @{ Image='diagnostics'; Title='04 / Test before real clicks'; Detail='Use test mode, check reference images and export diagnostics for support.'; Note='TEST MODE SENDS NO MOUSE INPUT' },
    @{ Image='hud-reference'; Title='05 / Confirm the join'; Detail='A run stops after five white HUD bars stay detected for at least 0.5 seconds.'; Note='EMBEDDED HUD REFERENCE / NO LIVE JOIN SHOWN' },
    @{ Image=$null; Title='06 / Stay up to date'; Detail='Release builds check at startup and every six hours. Your runtime variant and settings are preserved.'; Note='UPDATES ARE DISABLED IN THIS DOCUMENTATION CAPTURE' },
    @{ Image='about'; Title='07 / Your language, your project'; Detail='20 offline UI languages. English by default. Project, GPL license and bug reports in About the app.'; Note='OPEN SOURCE / GAMEWALKER ON GITHUB' }
)
$titleFont = New-Object System.Drawing.Font('Segoe UI', 30, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel))
$detailFont = New-Object System.Drawing.Font('Segoe UI', 26, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel))
$noteFont = New-Object System.Drawing.Font('Segoe UI', 18, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel))
$muted = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#b8c1ce'))
$accent = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#71c5ef'))
$white = [System.Drawing.Brushes]::White
$segments = @()
$subtitles = @()
try {
    for ($i = 0; $i -lt $scenes.Count; $i++) {
        $scene = $scenes[$i]
        $canvas = New-Object System.Drawing.Bitmap(1600, 1080)
        $graphics = [System.Drawing.Graphics]::FromImage($canvas)
        try {
            $graphics.Clear([System.Drawing.ColorTranslator]::FromHtml('#101419'))
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
            $graphics.DrawString($scene.Title, $titleFont, $white, 64, 30)
            $graphics.DrawString(('{0:00} / 08' -f ($i + 1)), $noteFont, $muted, 1440, 43)
            $graphics.DrawString($scene.Note, $noteFont, $accent, 64, 78)
            if ($scene.Image) {
                $shot = [System.Drawing.Image]::FromFile((Join-Path $captures ($scene.Image + '.png')))
                try {
                    $scale = [Math]::Min(1472.0 / $shot.Width, 790.0 / $shot.Height)
                    $width = [int]($shot.Width * $scale)
                    $height = [int]($shot.Height * $scale)
                    $graphics.DrawImage($shot, [int]((1600 - $width) / 2), [int](124 + (790 - $height) / 2), $width, $height)
                } finally { $shot.Dispose() }
            } else {
                $cards = @(
                    @('Verified downloads', 'Size, Windows EXE format and SHA-256 checks.'),
                    @('Update and restart', 'The header Update button appears when a newer version is available.'),
                    @('Install on exit', 'Or close the app to install the prepared update. The old EXE is kept as .previous.')
                )
                for ($card = 0; $card -lt $cards.Count; $card++) {
                    $y = 190 + 210 * $card
                    $graphics.DrawString($cards[$card][0], $titleFont, $white, 150, $y)
                    $graphics.DrawString($cards[$card][1], $detailFont, $muted, (New-Object System.Drawing.RectangleF(150, ($y + 60), 1280, 120)))
                }
            }
            $graphics.FillRectangle($accent, 64, 940, 64, 3)
            $graphics.DrawString($scene.Detail, $detailFont, $white, (New-Object System.Drawing.RectangleF(64, 966, 1472, 98)))
            $png = Join-Path $work "scene-$i.png"
            $canvas.Save($png, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $graphics.Dispose(); $canvas.Dispose() }
        $segment = Join-Path $work "scene-$i.mp4"
        & $Ffmpeg -hide_banner -loglevel error -y -loop 1 -framerate 30 -i $png -t 6 -vf 'fade=t=in:st=0:d=0.25,fade=t=out:st=5.75:d=0.25,format=yuv420p' -c:v libx264 -preset fast -crf 20 -an $segment
        if ($LASTEXITCODE -ne 0) { throw "Encoding scene $i failed." }
        $segments += "file '$($segment.Replace('\', '/').Replace("'", "'\''"))'"
        $start = [TimeSpan]::FromSeconds($i * 6).ToString('hh\:mm\:ss')
        $end = [TimeSpan]::FromSeconds(($i + 1) * 6).ToString('hh\:mm\:ss')
        $subtitles += "$($i + 1)`n${start},000 --> ${end},000`n$($scene.Title)`n$($scene.Detail)`n$($scene.Note)`n"
    }
    $list = Join-Path $work 'segments.txt'
    [IO.File]::WriteAllLines($list, $segments, (New-Object Text.UTF8Encoding($false)))
    $video = Join-Path $root 'docs/media/wardogs-teamselector-demo-en.mp4'
    & $Ffmpeg -hide_banner -loglevel error -y -f concat -safe 0 -i $list -c copy -movflags +faststart $video
    if ($LASTEXITCODE -ne 0) { throw 'Combining scenes failed.' }
    [IO.File]::WriteAllText((Join-Path $root 'docs/media/wardogs-teamselector-demo-en.srt'), ($subtitles -join "`n"), (New-Object Text.UTF8Encoding($false)))
    $gif = Join-Path $root 'docs/media/wardogs-teamselector-demo-en.gif'
    & $Ffmpeg -hide_banner -loglevel error -y -i $video -filter_complex 'fps=6,scale=800:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=3' -loop 0 $gif
    if ($LASTEXITCODE -ne 0) { throw 'GIF preview generation failed.' }
    Get-Item $video, $gif | Select-Object Name, Length
} finally { $titleFont.Dispose(); $detailFont.Dispose(); $noteFont.Dispose(); $muted.Dispose(); $accent.Dispose() }

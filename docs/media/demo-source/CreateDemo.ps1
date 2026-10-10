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
    @{ Image='operation'; Title='🐺 WardogsTeamselector'; Detail='Your team, one keypress. Play solo or bring your friends along.'; Note='ENGLISH FEATURE TOUR / NO LIVE MATCH' },
    @{ Image='setup'; Title='🎯 Set up your screen'; Detail='Find Wardogs, choose your monitor and line up the colored team outlines.'; Note='SAMPLE GAME SCREEN' },
    @{ Image='waiting-fixture'; Title='⌨️ Choose, switch or stop'; Detail='F6: Blue. F7: Red. F8: Green. Choose early, wait for the game, or stop with ESC.'; Note='DEMO STATUS / NO REAL CLICKS' },
    @{ Image='hud-reference'; Title='✅ Stop automatically after joining'; Detail='The app keeps trying your team and stops when it recognizes that you have joined.'; Note='SAMPLE IN-GAME SCREEN / NO LIVE JOIN' },
    @{ Image='share-team'; Title='👥 Share your team'; Detail='Turn on sharing, choose your group and pick a team. Your friends can follow your choice.'; Note='DEMO GROUP / NO ONLINE ACTIONS' },
    @{ Image='group-mode'; Title='🔁 Join once or keep following'; Detail='Join the shared team once, or use Auto follow to stay ready for the next team selection screen.'; Note='AUTO REMEMBERS YOUR CHOICE WHEN YOU REOPEN THE APP' },
    @{ Image='group-management'; Title='✉️ Invite and manage your friends'; Detail='Create groups, share invitations and approve requests. Manage members and your shared choice.'; Note='DEMO GROUP AND SAMPLE MEMBERS' },
    @{ Image='group-recovery'; Title='🔑 Take your groups with you'; Detail='Keep a private recovery code for a new PC. Copy admin access or transfer it exclusively.'; Note='KEEP RECOVERY AND ADMIN CODES PRIVATE' },
    @{ Image='configuration'; Title='💾 Make it yours'; Detail='Customize team and Stop keys, click speed and game focus. Your setup and checkbox choices are remembered.'; Note='YOUR SETTINGS / YOUR PLAY STYLE' },
    @{ Image='diagnostics'; Title='🧪 Test before you click'; Detail='Try test mode without real clicks. Check sample screens, save screenshots and export a report for help.'; Note='TEST MODE SENDS NO MOUSE INPUT' },
    @{ Image=$null; Title='🔄 Stay up to date'; Detail='Get new releases from the app. Your settings and download variant stay with you.'; Note='UPDATE OVERVIEW / NO UPDATE INSTALLED IN THIS TOUR' },
    @{ Image='about'; Title='🌍 Your language, your team'; Detail='20 interface languages, a dark Windows 11 look and project, license and bug-report links.'; Note='FREE AND OPEN SOURCE / GAMEWALKER ON GITHUB' }
)
$titleFont = New-Object System.Drawing.Font('Segoe UI Emoji', 30, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel))
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
            $graphics.DrawString(('{0:00} / {1:00}' -f ($i + 1), $scenes.Count), $noteFont, $muted, 1440, 43)
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
                    @('New versions, within reach', 'The app checks for updates regularly. You can check manually too.'),
                    @('Update and restart', 'Use the Update button when a new version is ready.'),
                    @('Keep your preferences', 'Or install a ready update when you close the app. Your settings are kept.')
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

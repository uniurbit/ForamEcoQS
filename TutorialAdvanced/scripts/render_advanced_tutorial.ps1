param(
    [string]$Ffmpeg = "ffmpeg",
    [string]$Ffprobe = "ffprobe"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$raw = Join-Path $root "TutorialAdvanced/source/advanced_workflow_raw.mkv"
$logo = Join-Path $root "ForamEcoQS/logo.png"
$callouts = Join-Path $root "TutorialAdvanced/overlays/advanced_callouts.ass"
$outputDir = Join-Path $root "TutorialAdvanced/output"
$output = Join-Path $outputDir "ForamEcoQS_Advanced_GUI_Tutorial_4K.mp4"
$thumbnail = Join-Path $outputDir "ForamEcoQS_Advanced_Tutorial_Thumbnail.png"

foreach ($required in @($raw, $logo, $callouts)) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Missing required file: $required" }
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$filter = Join-Path $env:TEMP "foramecoqs_advanced_filter.txt"
$assPath = $callouts.Replace("\", "/").Replace(":", "\:")
$fontPath = "C\:/Windows/Fonts/segoeui.ttf"
$fontBoldPath = "C\:/Windows/Fonts/seguisb.ttf"

$graph = @"
[1:v]split=2[logo_intro][logo_outro];
[2:v]format=rgba,drawbox=x=0:y=0:w=iw:h=ih:color=0x072B3A:t=fill,drawbox=x=0:y=1450:w=iw:h=710:color=0x0F6674:t=fill[intro_bg];
[logo_intro]scale=650:650:flags=lanczos[logo_i];
[intro_bg][logo_i]overlay=(W-w)/2:245:shortest=1,drawtext=fontfile='$fontBoldPath':text='ForamEcoQS Advanced':fontcolor=white:fontsize=142:x=(w-text_w)/2:y=1020,drawtext=fontfile='$fontPath':text='WoRMS - Custom Lists - Geographic Lists - Agreement Analysis':fontcolor=0xD8F5F4:fontsize=64:x=(w-text_w)/2:y=1250,drawtext=fontfile='$fontPath':text='GUI tutorial':fontcolor=white:fontsize=58:x=(w-text_w)/2:y=1850,fade=t=in:st=0:d=0.6,fade=t=out:st=8.3:d=0.7[intro];
[0:v]trim=start=0.5:end=230.5,setpts=PTS-STARTPTS,crop=w='if(between(t,12,31)+between(t,48,60)+between(t,61,143)+between(t,168,190)+between(t,191,230),2048,2560)':h='if(between(t,12,31)+between(t,48,60)+between(t,61,143)+between(t,168,190)+between(t,191,230),1152,1440)':x='if(between(t,34,48),0,if(between(t,12,31)+between(t,48,60)+between(t,61,143)+between(t,168,190)+between(t,191,230),256,0))':y='if(between(t,12,31)+between(t,48,60)+between(t,61,143)+between(t,168,190)+between(t,191,230),144,0)',scale=3840:2160:flags=lanczos,fps=30,subtitles='$assPath',format=yuv420p,fade=t=in:st=0:d=0.35,fade=t=out:st=229.3:d=0.7[workflow];
[3:v]format=rgba,drawbox=x=0:y=0:w=iw:h=ih:color=0x072B3A:t=fill,drawbox=x=0:y=1500:w=iw:h=660:color=0x0F6674:t=fill[outro_bg];
[logo_outro]scale=420:420:flags=lanczos[logo_o];
[outro_bg][logo_o]overlay=(W-w)/2:285:shortest=1,drawtext=fontfile='$fontBoldPath':text='Advanced workflow complete':fontcolor=white:fontsize=118:x=(w-text_w)/2:y=880,drawtext=fontfile='$fontPath':text='Verify taxa - document lists - compare classifications':fontcolor=0xD8F5F4:fontsize=64:x=(w-text_w)/2:y=1110,drawtext=fontfile='$fontPath':text='ForamEcoQS':fontcolor=white:fontsize=55:x=(w-text_w)/2:y=1860,fade=t=in:st=0:d=0.5,fade=t=out:st=6.3:d=0.7[outro];
[intro][workflow][outro]concat=n=3:v=1:a=0,format=yuv420p[outv]
"@

[System.IO.File]::WriteAllText($filter, $graph, (New-Object System.Text.UTF8Encoding($false)))
try {
    & $Ffmpeg -y -i $raw -loop 1 -framerate 30 -i $logo -f lavfi -t 9 -i "color=c=0x072B3A:s=3840x2160:r=30" -f lavfi -t 7 -i "color=c=0x072B3A:s=3840x2160:r=30" -filter_complex_script $filter -map "[outv]" -an -c:v libx264 -preset slow -crf 16 -profile:v high -level 5.2 -pix_fmt yuv420p -movflags +faststart $output
    if ($LASTEXITCODE -ne 0) { throw "FFmpeg render failed with exit code $LASTEXITCODE" }
    & $Ffprobe -v error -show_entries "format=duration,size:stream=codec_name,width,height,r_frame_rate,pix_fmt" -of default=noprint_wrappers=1 $output
    if ($LASTEXITCODE -ne 0) { throw "FFprobe validation failed" }

    $thumbFilter = "[0:v]scale=1280:720:flags=lanczos,eq=brightness=-0.12:saturation=0.95,drawbox=x=0:y=0:w=1280:h=720:color=0x032631@0.22:t=fill[bg];[1:v]scale=165:165:flags=lanczos[mark];[bg][mark]overlay=55:45,drawbox=x=35:y=420:w=1210:h=245:color=0x072B3A@0.90:t=fill,drawtext=fontfile='C\:/Windows/Fonts/seguisb.ttf':text='ADVANCED WORKFLOW':fontcolor=white:fontsize=72:x=(w-text_w)/2:y=450,drawtext=fontfile='C\:/Windows/Fonts/segoeui.ttf':text='WoRMS + LISTS + CONFUSION MATRIX':fontcolor=0x67E2DC:fontsize=47:x=(w-text_w)/2:y=555[thumb]"
    & $Ffmpeg -y -ss 237 -i $output -i $logo -filter_complex $thumbFilter -map "[thumb]" -frames:v 1 -update 1 $thumbnail
    if ($LASTEXITCODE -ne 0) { throw "Thumbnail render failed" }
}
finally {
    Remove-Item -LiteralPath $filter -Force -ErrorAction SilentlyContinue
}

Write-Host "Rendered: $output"
Write-Host "Thumbnail: $thumbnail"

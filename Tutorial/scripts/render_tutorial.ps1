param(
    [string]$Ffmpeg = "ffmpeg",
    [string]$Ffprobe = "ffprobe"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$raw = Join-Path $root "Tutorial/source/workflow_raw.mkv"
$logo = Join-Path $root "ForamEcoQS/logo.png"
$callouts = Join-Path $root "Tutorial/overlays/tutorial_callouts.ass"
$outputDir = Join-Path $root "Tutorial/output"
$output = Join-Path $outputDir "ForamEcoQS_GUI_Tutorial_4K.mp4"
$thumbnail = Join-Path $outputDir "ForamEcoQS_GUI_Tutorial_Thumbnail.png"

foreach ($required in @($raw, $logo, $callouts)) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Missing required file: $required" }
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$filter = Join-Path $env:TEMP "foramecoqs_tutorial_filter.txt"
$assPath = $callouts.Replace("\", "/").Replace(":", "\:")
$fontPath = "C\:/Windows/Fonts/segoeui.ttf"
$fontBoldPath = "C\:/Windows/Fonts/seguisb.ttf"

$graph = @"
[1:v]split=2[logo_intro][logo_outro];
[2:v]format=rgba,drawbox=x=0:y=0:w=iw:h=ih:color=0x072B3A:t=fill,drawbox=x=0:y=1450:w=iw:h=710:color=0x0F6674:t=fill[intro_bg];
[logo_intro]scale=700:700:flags=lanczos[logo_i];
[intro_bg][logo_i]overlay=(W-w)/2:260:shortest=1,drawtext=fontfile='$fontBoldPath':text='ForamEcoQS':fontcolor=white:fontsize=154:x=(w-text_w)/2:y=1080,drawtext=fontfile='$fontPath':text='GUI tutorial - From abundance matrix to ecological indices':fontcolor=0xD8F5F4:fontsize=66:x=(w-text_w)/2:y=1295,drawtext=fontfile='$fontPath':text='Foraminiferal Ecological Quality Status Assessment':fontcolor=white:fontsize=54:x=(w-text_w)/2:y=1840,fade=t=in:st=0:d=0.6,fade=t=out:st=6.3:d=0.7[intro];
[0:v]trim=start=18:end=196,setpts=PTS-STARTPTS,crop=w='if(between(t,4,16)+between(t,52,64)+between(t,69,82)+between(t,87,115)+between(t,128,150)+between(t,167,177),2048,2560)':h='if(between(t,4,16)+between(t,52,64)+between(t,69,82)+between(t,87,115)+between(t,128,150)+between(t,167,177),1152,1440)':x='if(between(t,29,48),512,if(between(t,52,64)+between(t,69,82)+between(t,87,115)+between(t,167,177),256,0))':y='if(between(t,52,64)+between(t,69,82)+between(t,87,115)+between(t,128,150)+between(t,167,177),144,0)',scale=3840:2160:flags=lanczos,fps=30,subtitles='$assPath',format=yuv420p,fade=t=in:st=0:d=0.35,fade=t=out:st=177.3:d=0.7[workflow];
[3:v]format=rgba,drawbox=x=0:y=0:w=iw:h=ih:color=0x072B3A:t=fill,drawbox=x=0:y=1500:w=iw:h=660:color=0x0F6674:t=fill[outro_bg];
[logo_outro]scale=430:430:flags=lanczos[logo_o];
[outro_bg][logo_o]overlay=(W-w)/2:300:shortest=1,drawtext=fontfile='$fontBoldPath':text='Your analysis is ready':fontcolor=white:fontsize=124:x=(w-text_w)/2:y=900,drawtext=fontfile='$fontPath':text='Keep plots focused - Export results - Report the selected settings':fontcolor=0xD8F5F4:fontsize=62:x=(w-text_w)/2:y=1140,drawtext=fontfile='$fontPath':text='ForamEcoQS 1.1.2':fontcolor=white:fontsize=55:x=(w-text_w)/2:y=1860,fade=t=in:st=0:d=0.5,fade=t=out:st=5.3:d=0.7[outro];
[intro][workflow][outro]concat=n=3:v=1:a=0,format=yuv420p[outv]
"@

[System.IO.File]::WriteAllText($filter, $graph, (New-Object System.Text.UTF8Encoding($false)))
try {
    & $Ffmpeg -y -i $raw -loop 1 -framerate 30 -i $logo -f lavfi -t 7 -i "color=c=0x072B3A:s=3840x2160:r=30" -f lavfi -t 6 -i "color=c=0x072B3A:s=3840x2160:r=30" -filter_complex_script $filter -map "[outv]" -an -c:v libx264 -preset slow -crf 16 -profile:v high -level 5.2 -pix_fmt yuv420p -movflags +faststart $output
    if ($LASTEXITCODE -ne 0) { throw "FFmpeg render failed with exit code $LASTEXITCODE" }
    & $Ffprobe -v error -show_entries "format=duration,size:stream=codec_name,width,height,r_frame_rate,pix_fmt" -of default=noprint_wrappers=1 $output
    if ($LASTEXITCODE -ne 0) { throw "FFprobe validation failed" }

    $thumbFilter = "[0:v]scale=1280:720:flags=lanczos,eq=brightness=-0.18:saturation=0.85,drawbox=x=0:y=0:w=1280:h=720:color=0x032631@0.30:t=fill[bg];[1:v]scale=175:175:flags=lanczos[mark];[bg][mark]overlay=55:50,drawbox=x=42:y=420:w=1196:h=240:color=0x072B3A@0.88:t=fill,drawtext=fontfile='C\:/Windows/Fonts/seguisb.ttf':text='FROM SPECIES COUNTS':fontcolor=white:fontsize=68:x=(w-text_w)/2:y=450,drawtext=fontfile='C\:/Windows/Fonts/segoeui.ttf':text='TO ECOLOGICAL INDICES':fontcolor=0x67E2DC:fontsize=56:x=(w-text_w)/2:y=550[thumb]"
    & $Ffmpeg -y -ss 170 -i $output -i $logo -filter_complex $thumbFilter -map "[thumb]" -frames:v 1 -update 1 $thumbnail
    if ($LASTEXITCODE -ne 0) { throw "Thumbnail render failed" }
}
finally {
    Remove-Item -LiteralPath $filter -Force -ErrorAction SilentlyContinue
}

Write-Host "Rendered: $output"
Write-Host "Thumbnail: $thumbnail"

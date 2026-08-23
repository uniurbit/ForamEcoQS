# ForamEcoQS GUI tutorial production bundle

This directory contains the synthetic dataset, production documents, capture and render scripts, source recording, rendered video, subtitles, and voiceover files for the English ForamEcoQS GUI tutorial. The edit opens with the official logo and uses restrained zooms so GUI controls stay sharp.

## Intended outputs

- `output/ForamEcoQS_GUI_Tutorial_4K.mp4`
- `output/ForamEcoQS_GUI_Tutorial_Thumbnail.png`
- `voiceover/voiceover_en.md`
- `voiceover/voiceover_segments.csv`
- `voiceover/subtitles_en.srt`
- `voiceover/translation_template.csv`

The final videos deliberately contain no audio. The timed English script is designed for later voice generation and translation with services such as ElevenLabs.

## Rebuild the 4K master

```powershell
powershell -ExecutionPolicy Bypass -File .\Tutorial\scripts\render_tutorial.ps1
```

The CSV is a genuine abundance matrix: rows are species, columns are `Sample_01` through `Sample_06`, and cells are integer counts. Mud percentages are entered separately through the GUI when TSI requests them.

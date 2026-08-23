# ForamEcoQS advanced GUI tutorial

This production bundle covers online WoRMS verification, user custom species lists, the geographic areas database, Cohen's kappa, and pairwise confusion matrices.

The capture workflow starts recording only after the ForamEcoQS main window fully covers the capture region. A dedicated neutral backdrop remains behind the application for the entire session, preventing other desktop activity from appearing around dialogs or during window transitions.

## Demonstration inputs

- `dataset/WoRMS_Demo_Dataset.csv`: species-count matrix containing locally matched names, WoRMS-only names, a synonym, and one intentionally fictional name.
- `dataset/Custom_Foram_AMBI_List.csv`: importable `Species;Value` custom list.
- `dataset/Geographic_Areas_Demo.csv`: two importable geographic/environmental reference records.
- `../Tutorial/dataset/ForamEcoQS_Tutorial_Dataset.csv`: the validated abundance matrix used to calculate EQS classes and the confusion matrix.

## Reproduce the tutorial

Run `scripts/record_advanced_workflow.ps1` to repeat the isolated GUI capture. Use `-DryRun` to exercise the complete workflow without recording. The script restores the Custom Lists manifest and declines persistent Geographic Lists changes when it exits.

Run `scripts/render_advanced_tutorial.ps1` to create the silent 4K YouTube master and 1280x720 thumbnail. FFmpeg and FFprobe must be available on `PATH`.

## Deliverables

- `output/ForamEcoQS_Advanced_GUI_Tutorial_4K.mp4`: 3840x2160, 30 fps, H.264 master ready for narration.
- `output/ForamEcoQS_Advanced_Tutorial_Thumbnail.png`: YouTube thumbnail.
- `voiceover/voiceover_segments.csv`: one ElevenLabs-ready text file name and time range per segment.
- `voiceover/voiceover_en.md`: continuous narration script with pronunciation notes.
- `voiceover/subtitles_en.srt`: timed English subtitles.
- `storyboard.md`: editorial sequence and visual intent.
- `production_manifest.md`: technical and scientific validation record.

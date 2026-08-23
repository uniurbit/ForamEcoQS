# Production manifest

- Delivery master: `output/ForamEcoQS_Advanced_GUI_Tutorial_4K.mp4`
- YouTube thumbnail: `output/ForamEcoQS_Advanced_Tutorial_Thumbnail.png` (1280x720)
- Resolution / frame rate: 3840x2160, 30 fps
- Codec: H.264 High Profile, CRF 16, `yuv420p`, fast-start MP4
- Duration: 4 minutes 6 seconds
- Audio: silent master; timed narration assets are supplied in `voiceover/`
- Screen source: 2560x1440, H.264, 20 fps, CRF 8 capture
- Scaling: Lanczos; maximum editorial zoom approximately 1.25x
- Opening asset: official repository logo, `ForamEcoQS/logo.png`
- Reproducible capture: `scripts/record_advanced_workflow.ps1`
- Reproducible render: `scripts/render_advanced_tutorial.ps1`
- Privacy: full capture region covered before recording; neutral backdrop remains active behind dialogs

## Demonstration validation

- WoRMS matrix uses taxa as rows and samples as columns; every sample totals 100 individuals.
- The fictional taxon is an intentional negative control.
- Custom-list import uses the application's `Species;Value` schema.
- Geographic entries are explicitly synthetic and are closed without persistence.
- The validated calculation matrix has 12 taxa, 6 samples, integer counts, and totals 300 individuals per sample.
- The agreement window is shown at a compact 1200x800 size rather than maximized.
- The final section generates a pairwise EQS confusion matrix.

## Scientific reference

Mangiagalli, M., Frontalini, F., Cristallo, C., & Francescangeli, F. (2026). ForamEcoQS: An analytical software suite for foraminiferal ecological quality status assessment. *SoftwareX, 35*, 102921. https://doi.org/10.1016/j.softx.2026.102921

## SHA-256 checksums

- Master: `906DAA170DF5CA0DABE3D832BC2CF5FC0758AC5380DB643ECF92A5D3486FA1F2`
- WoRMS dataset: `047FFCA49E61C9E693DF9D310C75464AF9E2750502CFD8979AEC2AB3A19D1436`
- Custom list: `C4FD1FB85E799BFA98B4FD9D0DF4A471A123672FA99BB728020FF227E3FC8D57`
- Geographic list: `E5B1DE7795C7B05D01B06C2B70902AA7B7449D477C0AE3B4201762B093D81A2B`

# Production manifest

- Delivery master: `output/ForamEcoQS_GUI_Tutorial_4K.mp4`
- YouTube thumbnail: `output/ForamEcoQS_GUI_Tutorial_Thumbnail.png` (1280×720)
- Resolution / frame rate: 3840×2160, 30 fps
- Codec: H.264 High Profile, CRF 16, `yuv420p`, fast-start MP4
- Audio: silent master; narration is supplied separately in `voiceover/`
- Screen source: 2560×1440, H.264, 20 fps, near-lossless CRF 8 capture
- Scaling: Lanczos; maximum editorial zoom approximately 1.25×
- Opening asset: official repository logo, `ForamEcoQS/logo.png`
- Synthetic input: `dataset/ForamEcoQS_Tutorial_Dataset.csv`
- Reproducible render: `scripts/render_tutorial.ps1`
- Master SHA-256: `E1712367D000D1A7EC8B3433A2FF19CA35A1987FF67B3E32EBB2F2512A68F296`
- Dataset SHA-256: `79F5A02F7D6A4E79424C889E72135DD0FE39CFC3E77F163FEC10D5EF4535CC4E`

## Validation

- 12 species and 6 sample columns
- all abundance cells are integers
- each sample totals 300 individuals
- mud values are entered separately in the TSI dialog
- final plot shows only Foram-AMBI, FSI, and exp(H'bc)

## Scientific reference

Mangiagalli, M., Frontalini, F., Cristallo, C., & Francescangeli, F. (2026). ForamEcoQS: An analytical software suite for foraminiferal ecological quality status assessment. *SoftwareX, 35*, 102921. https://doi.org/10.1016/j.softx.2026.102921

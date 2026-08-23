# ForamEcoQS tutorial dataset

`ForamEcoQS_Tutorial_Dataset.csv` is a deterministic synthetic abundance matrix created only for the ForamEcoQS GUI tutorial.

- The first column contains taxon names.
- The following columns are six artificial samples named `Sample_01` through `Sample_06`.
- Every matrix cell is an integer count of individuals for the corresponding species and sample.
- Each biological sample totals 300 individuals.
- The file uses a semicolon (`;`) separator.

The taxa were selected because they occur in the bundled Jorissen Foram-AMBI and FSI reference lists. Without encoding an ecological interpretation in the sample names, the synthetic assemblage shifts progressively from ecological groups 1–2 in `Sample_01` toward groups 4–5 in `Sample_06`. This makes changes in indices, ecological-quality classes, and plots easy to see.

The tutorial enters the separate TSI-Med mud percentages (`15`, `20`, `35`, `50`, `70`, and `85`) through the dedicated GUI dialog. They are intentionally not stored as a pseudo-species row in the abundance matrix.

These data are entirely synthetic. They must not be cited, interpreted, or reused as field observations or scientific results.

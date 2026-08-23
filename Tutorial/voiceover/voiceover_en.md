# ForamEcoQS GUI tutorial — English voiceover

Voice profile: clear scientific narration, neutral international English, approximately 125–135 words per minute. Import one section at a time into ElevenLabs or another text-to-speech service, preserving the listed time range. Total duration: 3 minutes 11 seconds.

## 00:00–00:11 — Opening

Welcome to ForamEcoQS. In this tutorial, we will turn a benthic-foraminiferal abundance matrix into calculated ecological indices and a focused comparison plot.

## 00:11–00:29 — Open the dataset

From the File menu, choose Open and select the semicolon-separated tutorial CSV. Each row is a species, each column is a sampling station, and every data cell is an integer abundance count. The sample names are neutral identifiers.

## 00:29–00:57 — Choose and compare the databank

Select the ecological-group databank appropriate for your study. Here we use the Jorissen classification. Before calculating, run the species comparison. It highlights names that need attention and helps prevent unmatched taxa from silently affecting the analysis. Clean or normalize names only after reviewing this comparison.

## 00:57–01:23 — Calculation settings and indices

Open Index Calculation Settings from the Tools menu, review the parameters, and save. Next, choose the indices required for the calculation. Later, we will plot only three of them. Selecting every available series would make the final figure crowded and much harder to interpret.

## 01:23–02:10 — Add sediment information

Some calculations need information that is not part of the abundance matrix. For the TSI workflow, ForamEcoQS asks for the mud percentage of every sample. Enter the values in sample order: fifteen, twenty, thirty-five, fifty, seventy, and eighty-five percent. These values provide a controlled environmental gradient. Notice that they are entered in their own dialog; they are not stored as a fake species row in the CSV.

## 02:10–02:35 — Review results

When calculation is complete, open the advanced results window. Use the Results table to inspect numerical values, then use the EQS view to review ecological quality classes. When reporting an analysis, record the selected databank, calculation settings, and environmental inputs.

## 02:35–03:03 — Build a readable plot

Move to the Plot tab. For this focused demonstration, select only Foram-AMBI, FSI, and exponential H-prime b-c. Choose Line Plot and generate the chart. Limiting the selection preserves legibility and makes the pattern across the six samples immediately visible.

## 03:03–03:11 — Closing

Use the Composite Panel for an overview. Keep plots focused, export the results you need, and document every setting used.

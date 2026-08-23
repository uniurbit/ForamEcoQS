# ForamEcoQS advanced tutorial voiceover

Voice direction: clear scientific YouTube tutorial, friendly and measured, approximately 120 words per minute. Read “WoRMS” as “worms”, “EQS” as the letters E-Q-S, and “Aphia ID” as “A-fee-ah I-D”. Leave a short pause between sections.

Welcome back to ForamEcoQS. In this advanced GUI tutorial, we will verify species with WoRMS, import a custom ecological list, explore geographic reference lists, and compare ecological-quality classifications with Cohen's kappa and a confusion matrix.

First, open the synthetic abundance matrix. Its rows are foraminiferal species and its columns are samples. Every value is an integer count, and each sample totals one hundred individuals. Choose the Jorissen databank, then open Index Calculation Settings and enable online WoRMS verification.

Run Compare Species. ForamEcoQS checks names against the selected local list and, when needed, queries the World Register of Marine Species. The colored rows make the review fast: familiar taxa match locally, accepted WoRMS records can be identified online, a synonym can point to its accepted name, and the fictional tutorial taxon remains unresolved. Always review these results before editing scientific names.

Now open Custom Lists. Select Import and choose the supplied Species-and-Value CSV. The first column contains species names; the second contains ecological-group values from one to five. After import, select the tutorial list and inspect its entries. A custom list is useful when a project follows a reviewed regional classification that is not bundled with the application. Give every list a clear name and retain its source and version with your analysis records.

Next, open Geographic Lists. Import the tutorial CSV and filter the table to the synthetic Adriatic examples. Geographic entries associate an area and environmental context with a reference and notes. They help users find a defensible list for a study area; they do not replace expert taxonomic review. The demonstration records are explicitly synthetic, so do not use them as scientific evidence. Close without saving to leave the user's existing database unchanged.

Reload the validated tutorial abundance matrix, compare its species, and calculate the selected indices. Enter the six mud percentages in sample order when requested. The advanced results window gathers numerical values, E-Q-S classifications, plots, and agreement tools in one place.

Open E-Q-S Agreement Analysis. The compact colored matrix reports pairwise Cohen's kappa values, so agreement among classifications can be scanned without covering the whole desktop. Color is a guide, while the numerical kappa value is the result to report. Interpret agreement together with sample size and the class distribution.

Finally, open the Confusion Matrices tab, choose two classifications, and generate the matrix. Rows and columns show how samples move between E-Q-S classes; diagonal cells are agreements and off-diagonal cells are disagreements. Export the result you need, document all settings and source lists, and keep the analysis reproducible.

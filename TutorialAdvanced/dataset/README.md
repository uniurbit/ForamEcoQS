# Advanced tutorial synthetic data

`WoRMS_Demo_Dataset.csv` is a true abundance matrix: rows are taxa, columns are samples, and every cell is an integer count. Each sample totals 100 individuals.

The online-verification rows were selected to demonstrate distinct states:

- `Globigerina bulloides`: accepted WoRMS taxon, absent from the selected local Jorissen ecological list.
- `Globigerina pachyderma`: WoRMS synonym whose accepted name is `Neogloboquadrina pachyderma`.
- `Orbulina universa`: accepted WoRMS taxon, absent from the selected local list.
- `Foraminifera tutorialensis`: intentionally fictional negative-control name.

WoRMS is an online service, so the live demonstration requires internet access. The verified AphiaIDs at production time were 113434, 113438, and 113460 respectively for the three real names.

The custom-list and geographic-area CSV files use the import schemas expected by their respective GUI managers. Both geographic records are explicitly synthetic tutorial entries.

# ForamEcoQS

ForamEcoQS is a Windows desktop application for ecological quality assessment based on benthic foraminifera. It targets `.NET 10`, uses Windows Forms for the graphical interface, and can also run in command-line mode. The software calculates multiple biotic and diversity indices from species-by-sample abundance matrices and assigns Ecological Quality Status (EQS) classes where implemented.

## Authors and Affiliations

### Department of Pure and Applied Sciences (DiSPeA), University of Urbino Carlo Bo (UniUrb)

- Matteo Mangiagalli (<m.mangiagalli@campus.uniurb.it>)
- Fabrizio Frontalini (<fabrizio.frontalini@uniurb.it>)
- Carla Cristallo (<c.cristallo1@campus.uniurb.it>)

### Department of Geosciences, University of Fribourg (UNIFR)

- Fabio Francescangeli (<fabio.francescangeli@unifr.ch>)

Year: `2026`

## Overview

ForamEcoQS supports a practical analysis workflow:

- Import abundance matrices from Excel or CSV files;
- Create or edit sample tables directly in the application;
- Clean and normalize sample data;
- Calculate selected indices for each sample;
- Review EQS classifications for supported indices;
- Export tables, plots, and EQS agreement summaries.

## Main Features

### Data Input and Editing

- Create an empty dataset.
- Open existing datasets from Excel or CSV.
- Save the current dataset to Excel.
- Create spreadsheet templates from reference lists.
- Add, remove, and rename sample columns.
- Undo recent data edits.
- Clean and normalize sample values.

### Index Calculation

- Compute selected indices from the `Advanced Indices` workflow.
- Choose threshold systems and reference options from `Index Calculation Settings`.
- Apply optional EQR calculations for `FSI` and `exp(H'bc)`.
- Use selected Foram-AMBI lists for eco-group based indices.

### Visualization and Export

- Open plot options for calculated results.
- Open a composite dashboard.
- Load existing index tables for plotting without recalculation.
- Export result tables to Excel.
- Export plots to PNG.

### Reference Lists Management

- Foram-AMBI List Manager.
- FSI List Manager.
- Geographic Areas Database.
- User Custom Lists Manager.
- Ecological-group override support for taxa assignments.

### EQS Comparison Tools

- EQS summary table.
- Pairwise Cohen's kappa matrix.
- Confusion matrices between indices.
- Kappa heatmap.
- Exportable EQS agreement summary.

### CLI Mode

- Run the same calculator in command-line mode by passing arguments at startup.
- Read the first worksheet from an Excel input file.
- Print results to console or export them to Excel.

## Calculated Indices

### Core Ecological / Biotic Indices

- Foram-AMBI
- Foram-M-AMBI
- FSI
- TSI-Med
- NQIf
- FIEI
- BENTIX
- BQI
- FoRAM Index

### Diversity / Structure Metrics

- `exp(H'bc)`
- `H'log2`
- `H'ln`
- `Simpson (1-D)`
- `Pielou's J`
- `ES100`

### Supporting Metrics

- Species Richness (`S`)
- Total Abundance (`N`)
- Ecological group percentages (`EG1`-`EG5`)
- FoRAM functional group percentages

## Key Thresholds and Criteria Used by the Software

### Foram-AMBI EQS Threshold Systems

The threshold system is selectable in `Index Calculation Settings`.

| System | High | Good | Moderate | Poor | Bad |
| --- | --- | --- | --- | --- | --- |
| Borja 2003 | `<= 1.2` | `<= 3.3` | `<= 4.3` | `<= 5.5` | `> 5.5` |
| Parent 2021 | `< 1.4` | `< 2.4` | `< 3.4` | `< 4.4` | `>= 4.4` |
| Bouchet 2025 (Brazilian transitional waters) | `< 1.4` | `< 1.8` | `< 3.0` | `<= 4.0` | `> 4.0` |

### TSI-Med Criteria

Implemented formula:

`TSI-Med = ((%TS - %TSref) / (100 - %TSref)) x 100`

`%TSref` is derived from mud percentage (`%mud`) using the selected reference curve:

- Barras 2014: `%TSref = 5.0 + 0.3 x %mud`
- Parent 2021 (>125 µm): `%TSref = 4.5 + 0.28 x %mud`
- Jorissen 2018 homogenized: `%TSref = 3.6718 + 0.3247 x %mud`

Supported EQS conventions:

| Convention | High | Good | Moderate | Poor | Bad |
| --- | --- | --- | --- | --- | --- |
| Parent 2021 | `<= 4` | `<= 16` | `<= 36` | `<= 64` | `> 64` |
| Barras & Jorissen 2011 | `> 64` | `<= 64` | `<= 36` | `<= 16` | `<= 4` |

The second convention inverts the ecological meaning of low TSI values.

### FSI EQS

| Class | Rule |
| --- | --- |
| High | `>= 9` |
| Good | `>= 5.5` |
| Moderate | `>= 2` |
| Poor | `>= 1` |
| Bad | `< 1` |

`0` is treated as `Azoic`.

### NQIf EQS

| Class | Rule |
| --- | --- |
| High | `>= 0.54` |
| Good | `>= 0.45` |
| Moderate | `>= 0.31` |
| Poor | `>= 0.13` |
| Bad | `< 0.13` |

### `exp(H'bc)` Threshold Sets

| Threshold Set | High | Good | Moderate | Poor | Bad |
| --- | --- | --- | --- | --- | --- |
| O'Brien 2021 Norway `>125 µm` | `>= 10` | `>= 7` | `>= 5` | `> 2` | `<= 2` |
| O'Brien 2021 Norway `>63 µm` | `>= 22` | `>= 13` | `>= 7` | `> 3` | `<= 3` |
| O'Brien 2021 Italy `>63 µm` | `>= 5` | `>= 4` | `>= 3` | `> 2` | `<= 2` |

### EQR Logic

Optional EQR mode is available for `FSI` and `exp(H'bc)`.

- `EQR = Observed / Reference`
- The value is clamped to the `0.0-1.0` range.

EQS boundaries from EQR:

| Class | Rule |
| --- | --- |
| High | `>= 0.8` |
| Good | `>= 0.6` |
| Moderate | `>= 0.4` |
| Poor | `>= 0.2` |
| Bad | `< 0.2` |

Default reference values exposed by the settings dialog:

- `FSI` reference value: `10.0`
- `exp(H'bc)` reference value: `20.0`

### Other Implemented EQS Classifications

| Index | High | Good | Moderate | Poor | Bad |
| --- | --- | --- | --- | --- | --- |
| BENTIX | `>= 4.5` | `>= 3.5` | `>= 2.5` | `>= 2.0` | `< 2.0` |
| BQI | `>= 12` | `>= 8` | `>= 5` | `>= 2` | `< 2` |
| Foram-M-AMBI | `>= 0.81` | `>= 0.61` | `>= 0.41` | `>= 0.21` | `< 0.21` |

For `BENTIX` and `BQI`, `0` is treated as `Azoic`.

### FoRAM Index Interpretation

Implemented formula:

`FI = (10 x Ps) + Po + (2 x Ph)`

Where:

- `Ps` = symbiont-bearing proportion
- `Po` = stress-tolerant proportion
- `Ph` = heterotrophic proportion

Interpretation:

- `> 4`: suitable for coral growth
- `2-4`: marginal conditions
- `< 2`: unsuitable for coral growth

## Requirements

- [`.NET 10 SDK`](https://dotnet.microsoft.com/download/dotnet/10.0) for build and run from source. Confirm the installation with `dotnet --list-sdks`; a `10.0.x` entry is required.
- Windows 10 or Windows 11 for normal GUI execution. The application target is `net10.0-windows`.
- Bundled reference `.csv` and `.xls` files available at runtime.

NuGet packages used by the project:

- `ClosedXML`
- `ExcelDataReader`
- `ExcelDataReader.DataSet`
- `OxyPlot.Core`
- `OxyPlot.WindowsForms`
- `System.Data.DataSetExtensions`

Important build note:

- The project targets `net10.0-windows`.
- On non-Windows systems, `dotnet build` can fail with `NETSDK1100` unless Windows targeting is enabled explicitly.

## Windows Installation Tutorials

These instructions build and run the application from this repository. They apply to 64-bit Windows installations; use PowerShell or Windows Terminal.

### Windows 11

1. Install the [`.NET 10 SDK`](https://dotnet.microsoft.com/download/dotnet/10.0), then close and reopen the terminal so its `PATH` is refreshed.
2. Clone the repository and enter its directory:

   ```powershell
   git clone https://github.com/uniurbit/ForamEcoQS.git
   cd ForamEcoQS
   ```

3. Confirm that an SDK in the `10.0.x` series is available:

   ```powershell
   dotnet --list-sdks
   ```

4. Restore, build, and start the GUI:

   ```powershell
   dotnet restore ForamEcoQS.sln
   dotnet build ForamEcoQS.sln -c Release
   dotnet run --project ForamEcoQS -c Release
   ```

### Windows 10

1. Install all pending Windows updates, then install the [`.NET 10 SDK`](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Open a new PowerShell window and verify the SDK:

   ```powershell
   dotnet --info
   ```

   The output must list a `10.0.x` SDK.
3. Follow steps 2–4 in the Windows 11 tutorial above. The commands and expected behavior are identical on Windows 10.

For users who only need to run a future packaged release, install the matching .NET Desktop Runtime rather than the SDK. Building from this source repository always requires the SDK.

## Build

Standard build:

```bash
dotnet restore ForamEcoQS.sln
dotnet build ForamEcoQS.sln -c Release
```

On non-Windows systems:

```bash
dotnet build ForamEcoQS.sln -c Release -p:EnableWindowsTargeting=true
```

This can build the Windows-targeted application, but GUI execution is still intended for Windows.

## Run

### Run the GUI

```bash
dotnet run --project ForamEcoQS
```

If no command-line arguments are passed, the application starts in GUI mode, shows the splash screen, and then opens the main window.

### Run the CLI

```bash
dotnet run --project ForamEcoQS -- -i INPUT_FILE [options]
```

Supported options:

- `-i INPUT_FILE`
- `-index=INDEX_LIST`
- `-list LIST_NAME`
- `-o OUTPUT_FILE`
- `-mud=VALUE`
- `-help`
- `--help`
- `/?`

Supported reference list names for `-list`:

- `jorissen`
- `alve`
- `bouchetmed`
- `bouchetatl`
- `bouchetsouthatl`
- `OMalley2021`

Accepted index names in `-index=`:

- `exp(H'bc)`
- `H'log2`
- `H'ln`
- `FSI`
- `TSI-Med`
- `NQIf`
- `FIEI`
- `Foram-AMBI`
- `Foram-M-AMBI`
- `BENTIX`
- `BQI`
- `FoRAM Index`
- `Species Richness (S)`
- `Total Abundance (N)`
- `Simpson (1-D)`
- `Pielou's J`
- `ES100`

Example:

```bash
dotnet run --project ForamEcoQS -- -i data.xlsx -index=all -list jorissen -o results.xlsx -mud=50
```

## Worked CLI Verification Example

Use this small, deterministic example to verify that the CLI, input parsing, and Excel export work after installation.

1. Create an Excel workbook named `verification-input.xlsx`. Its first worksheet must contain the following table, with the first row used as headers:

   | Species | Sample_A | Sample_B |
   | --- | ---: | ---: |
   | Taxon alpha | 10 | 0 |
   | Taxon beta | 20 | 25 |
   | Taxon gamma | 0 | 5 |

2. From the repository root, run:

   ```powershell
   dotnet run --project ForamEcoQS -c Release -- -i .\verification-input.xlsx "-index=Species Richness (S),Total Abundance (N)" -o .\verification-results.xlsx
   ```

3. The command must finish with `Results saved.` and `Done.`. Open the `Results` worksheet in `verification-results.xlsx` and verify these values:

   | Index | Sample_A | Sample_B |
   | --- | ---: | ---: |
   | Species Richness (S) | 2 | 2 |
   | Total Abundance (N) | 30 | 30 |

This example intentionally uses indices that do not require a reference databank. To calculate reference-list-dependent indices such as `Foram-AMBI`, add `-list jorissen` (or another supported list) to the command.

## Input File Format

The expected input layout is a species-by-sample matrix:

- The first column must contain species names;
- Each following column represents one sample;
- Positive numeric values are treated as abundances.

CLI mode supports Excel `.xls` and `.xlsx` files and reads the first worksheet. GUI mode also supports CSV import.

## Troubleshooting

| Symptom | Cause | Resolution |
| --- | --- | --- |
| `dotnet` is not recognized | The SDK is not installed or the current terminal has an outdated `PATH`. | Install the .NET 10 SDK, close every terminal window, open a new PowerShell window, and run `dotnet --info`. |
| `NETSDK1045` or an error saying `net10.0-windows` is unsupported | An older SDK is being used. | Install .NET 10 SDK and ensure `dotnet --list-sdks` shows `10.0.x`. Remove or update any repository/user `global.json` that pins an older SDK. |
| `NETSDK1100` on macOS or Linux | The project is Windows-targeted. | Build with `-p:EnableWindowsTargeting=true`; run the WinForms GUI on Windows. |
| `Error: Input file ... not found` | The `-i` path is incorrect or not quoted. | Use an absolute path or quote a path containing spaces, for example `-i "C:\Data\input file.xlsx"`. |
| `Error: Input file is empty or invalid` | The file is not a readable Excel workbook, has no rows, or its first worksheet has no header row. | Save the data as `.xls` or `.xlsx`, place species names in the first column, and include a header row. |
| `Error: Could not load reference databank` | The list name is invalid or the application is not run from its build/project output. | Use one of the documented list names (for example `jorissen`) and run with `dotnet run --project ForamEcoQS` so bundled data files are copied to the output directory. |
| Excel export fails because the file is in use | `results.xlsx` is open in Excel or another application. | Close the file, choose a different output name, and rerun the command. |

## Output and Exports

ForamEcoQS can produce:

- Calculated result tables in the `Advanced Indices` window;
- An EQS summary table;
- Excel exports of results;
- PNG exports for generated plots;
- Exports for EQS agreement outputs, including the kappa matrix.

## Tools and Databanks

The application includes:

- Selectable Foram-AMBI reference lists;
- A Foram-AMBI List Manager;
- An FSI List Manager;
- A Geographic Areas Database;
- A User Custom Lists Manager;
- Taxon-specific ecological-group overrides;
- Template generation from lists.

## Notes and Limitations

- The GUI is Windows-oriented because the project is a Windows Forms application.
- Some indices depend on the selected databank and species classification coverage.
- TSI-Med depends on mud percentage and the selected reference curve.
- FoRAM Index is mainly meaningful for tropical or subtropical reef settings.
- `Azoic` is treated separately from `Bad` in several EQS outputs.

## References

- Borja A. et al. (2003). AMBI classification boundaries.
- Parent B. et al. (2021). Foram-AMBI and TSI-related threshold updates.
- Bouchet V.M.P. et al. (2025). Brazilian transitional waters Foram-AMBI thresholds.
- Barras C. et al. (2014). TSI-Med reference relationship.
- Jorissen F.J. et al. (2018). Foram-AMBI Mediterranean development.
- O'Brien B.J. et al. (2021). `exp(H'bc)` thresholds and related foraminiferal EQS work.
- Dimiza M.D. et al. (2016). Foram Stress Index.
- Alve E. et al. (2019). Norwegian Quality Index for foraminifera.
- Simboura N. and Zenetos A. (2002). BENTIX.
- Rosenberg R. et al. (2004). BQI framework.
- Muxika I. et al. (2007). M-AMBI / multivariate ecological quality approach.
- Hallock P. et al. (2003). Original FoRAM Index.
- Prazeres M. et al. (2020). FoRAM Index revisit.
- Mojtahid M. et al. (2006). FIEI.

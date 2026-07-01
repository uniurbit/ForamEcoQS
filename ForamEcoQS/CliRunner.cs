using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using ExcelDataReader;

namespace ForamEcoQS
{
    public static class CliRunner
    {
        private static void Log(string message)
        {
             try { Console.WriteLine(message); } catch {}
        }

        public static int Run(string[] args)
        {
            try
            {
                Log("Starting CLI Runner...");
                var options = ParseArguments(args);
                Log("Arguments parsed.");

                if (options == null)
                {
                    ShowHelp();
                    return 1;
                }

                if (string.IsNullOrEmpty(options.InputFile))
                {
                    Log("Error: Input file is required (-i).");
                    ShowHelp();
                    return 1;
                }

                if (!File.Exists(options.InputFile))
                {
                    Log($"Error: Input file '{options.InputFile}' not found.");
                    return 1;
                }

                Log($"Processing input file: {options.InputFile}");
                DataTable inputData = LoadInputExcel(options.InputFile);
                if (inputData == null || inputData.Rows.Count == 0)
                {
                    Log("Error: Input file is empty or invalid.");
                    return 1;
                }

                // Extract an optional per-sample mud (%) row (e.g. "Mud (%)", "Fango") so it is used
                // for TSI-Med instead of being mistakenly counted as a species abundance row.
                var mudPercentagesPerSample = SpecializedDatabankLoader.ExtractMudRowFromDataTable(inputData);
                if (mudPercentagesPerSample != null)
                {
                    Log("Found a mud (%) row in the input file; using its per-sample values for TSI-Med " +
                        $"(overrides -mud={options.MudPercentage} for samples it covers).");
                }

                Log($"Loaded {inputData.Rows.Count} species rows and {inputData.Columns.Count - 1} samples.");

                // Load Reference Databank
                DataTable refDatabank = null;
                if (!string.IsNullOrEmpty(options.ReferenceList))
                {
                    Log($"Loading reference databank: {options.ReferenceList}");
                    refDatabank = LoadReferenceDatabank(options.ReferenceList);
                    if (refDatabank == null)
                    {
                        Log($"Error: Could not load reference databank '{options.ReferenceList}'.");
                        return 1;
                    }
                    Log("Reference databank loaded.");
                }
                else
                {
                    Log("Warning: No reference list specified (-list). F-AMBI and related indices will not be calculated correctly.");
                }

                // Report species not found in the reference databank (excluded from eco-group-based
                // indices such as Foram-AMBI). Optionally verified online against WoRMS with -worms.
                if (refDatabank != null)
                {
                    if (!string.IsNullOrEmpty(options.EcoOverridesFile))
                    {
                        options.EcoOverrides = LoadIntegerOverrides(options.EcoOverridesFile, "ecological-group");
                        ApplyEcoOverridesToDatabank(refDatabank, options.EcoOverrides);
                        Log($"Loaded {options.EcoOverrides.Count} ecological-group override(s) from: {options.EcoOverridesFile}");
                    }

                    ReportUnmatchedSpecies(inputData, refDatabank, options.UseWorms);
                }
                else if (!string.IsNullOrEmpty(options.EcoOverridesFile))
                {
                    Log("Warning: -overrides was supplied without -list; ecological-group overrides require a reference list and will be ignored.");
                }

                if (!string.IsNullOrEmpty(options.FsiOverridesFile))
                {
                    options.FsiOverrides = LoadCategoryOverrides(options.FsiOverridesFile, "FSI", NormalizeFsiCategory);
                    Log($"Loaded {options.FsiOverrides.Count} FSI override(s) from: {options.FsiOverridesFile}");
                }

                if (!string.IsNullOrEmpty(options.ForamOverridesFile))
                {
                    options.ForamOverrides = LoadCategoryOverrides(options.ForamOverridesFile, "FoRAM", NormalizeForamCategory);
                    Log($"Loaded {options.ForamOverrides.Count} FoRAM Index override(s) from: {options.ForamOverridesFile}");
                }

                // Calculate Indices
                Log("Calculating indices...");
                DataTable results = CalculateIndices(inputData, refDatabank, options, mudPercentagesPerSample);
                Log("Indices calculated.");

                // Output
                if (!string.IsNullOrEmpty(options.OutputFile))
                {
                    Log($"Saving results to: {options.OutputFile}");
                    SaveToExcel(results, options.OutputFile);
                    Log("Results saved.");
                }
                else
                {
                    PrintToConsole(results);
                }

                Log("Done.");
                return 0;
            }
            catch (Exception ex)
            {
                Log($"An unexpected error occurred: {ex.Message}");
                Log(ex.StackTrace);
                return 1;
            }
        }

        private static CliOptions ParseArguments(string[] args)
        {
            var options = new CliOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "-i" && i + 1 < args.Length)
                {
                    options.InputFile = args[++i];
                }
                else if (arg.StartsWith("-index="))
                {
                    options.Indices = arg.Substring("-index=".Length);
                }
                else if (arg == "-list" && i + 1 < args.Length)
                {
                    options.ReferenceList = args[++i];
                }
                else if (arg == "-o" && i + 1 < args.Length)
                {
                    options.OutputFile = args[++i];
                }
                else if (arg.StartsWith("-mud="))
                {
                    if (double.TryParse(arg.Substring("-mud=".Length), out double mud))
                    {
                        options.MudPercentage = mud;
                    }
                }
                else if (arg == "-worms")
                {
                    options.UseWorms = true;
                }
                else if ((arg == "-overrides" || arg == "-eg-overrides") && i + 1 < args.Length)
                {
                    options.EcoOverridesFile = args[++i];
                }
                else if (arg.StartsWith("-overrides="))
                {
                    options.EcoOverridesFile = arg.Substring("-overrides=".Length);
                }
                else if (arg.StartsWith("-eg-overrides="))
                {
                    options.EcoOverridesFile = arg.Substring("-eg-overrides=".Length);
                }
                else if (arg == "-fsi-overrides" && i + 1 < args.Length)
                {
                    options.FsiOverridesFile = args[++i];
                }
                else if (arg.StartsWith("-fsi-overrides="))
                {
                    options.FsiOverridesFile = arg.Substring("-fsi-overrides=".Length);
                }
                else if (arg == "-fsi-overrides-replace")
                {
                    options.ReplaceFsiWithOverrides = true;
                }
                else if (arg == "-foram-overrides" && i + 1 < args.Length)
                {
                    options.ForamOverridesFile = args[++i];
                }
                else if (arg.StartsWith("-foram-overrides="))
                {
                    options.ForamOverridesFile = arg.Substring("-foram-overrides=".Length);
                }
                else if (arg == "-help" || arg == "--help" || arg == "/?")
                {
                    return null;
                }
            }
            return options;
        }

        private static void ShowHelp()
        {
            Log("ForamEcoQS CLI Usage:");
            Log("  foramecoqs -i INPUT_FILE [options]");
            Log("");
            Log("Options:");
            Log("  -i INPUT_FILE         Path to input Excel file (.xls, .xlsx). First column must be species names.");
            Log("  -index=INDEX_LIST     Comma-separated list of indices to calculate, or 'all'.");
            Log("                        Available: 'exp(H'bc)', 'H'log2', 'H'ln', 'FSI', 'TSI-Med', 'NQIf', 'FIEI', 'Foram-AMBI', 'Foram-M-AMBI', 'BENTIX', 'BQI', 'FoRAM Index', 'Species Richness (S)', 'Total Abundance (N)', 'Simpson (1-D)', 'Pielou's J', 'ES100'");
            Log("  -list LIST_NAME       Reference databank to use (e.g., 'jorissen', 'alve', 'bouchetmed', 'bouchetatl', 'bouchetsouthatl', 'OMalley2021').");
            Log("  -o OUTPUT_FILE        Path to output Excel file. If omitted, prints to console.");
            Log("  -mud=VALUE            Percentage of mud (grains < 63µm) for TSI-Med calculation (default: 50). Applies to all samples.");
            Log("  -overrides FILE       Apply manual ecological-group overrides from JSON or CSV (Species;Ecogroup).");
            Log("  -fsi-overrides FILE   Apply FSI category overrides from JSON or CSV (Species;S/T or Sen/Str).");
            Log("  -fsi-overrides-replace Use the supplied FSI override file as the complete FSI list.");
            Log("  -foram-overrides FILE Apply FoRAM functional-group overrides from JSON or CSV (Species;SB/ST/SH/H).");
            Log("  -worms                Verify species not found in the reference databank against WoRMS");
            Log("                        (World Register of Marine Species, marinespecies.org). Requires internet access.");
            Log("");
            Log("Example:");
            Log("  foramecoqs -i data.xlsx -index=all -list jorissen -o results.xlsx -worms");
        }

        private static DataTable LoadInputExcel(string filePath)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });
                return result.Tables.Count > 0 ? result.Tables[0] : null;
            }
        }

        /// <summary>
        /// Reports species from the input file that were not found in the reference databank.
        /// These species are excluded from eco-group-based indices (Foram-AMBI, Foram-M-AMBI,
        /// NQIf, FIEI, BENTIX, BQI) but still count towards diversity indices and total abundance.
        /// When <paramref name="useWorms"/> is set, each unmatched name is additionally verified
        /// against the WoRMS (World Register of Marine Species) online database, logging whether
        /// it is a recognized valid marine taxon, an outdated synonym (with the accepted name), or
        /// unrecognized altogether. Purely diagnostic: it does not alter the calculated indices.
        /// </summary>
        private static void ReportUnmatchedSpecies(DataTable inputData, DataTable refDatabank, bool useWorms)
        {
            var databankValues = refDatabank.Rows.Cast<DataRow>()
                .Select(r => r["Species"]?.ToString()?.Trim())
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => v!)
                .ToList();
            var speciesMatcher = new SpeciesNameMatcher(databankValues);

            var unmatchedNames = inputData.Rows.Cast<DataRow>()
                .Select(r => r[0]?.ToString()?.Trim())
                .Where(n => !string.IsNullOrEmpty(n) && !speciesMatcher.IsMatch(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (unmatchedNames.Count == 0)
            {
                Log("Taxon check: all species were found in the reference databank.");
                return;
            }

            Log($"Warning: {unmatchedNames.Count} species not found in the reference databank " +
                "(excluded from Foram-AMBI and other eco-group-based indices): " +
                string.Join(", ", unmatchedNames.Take(20)) +
                (unmatchedNames.Count > 20 ? ", ..." : ""));

            if (!useWorms)
            {
                Log("Tip: pass -worms to verify these names against the WoRMS online database.");
                return;
            }

            Log($"WoRMS check: verifying {unmatchedNames.Count} species not found in the reference databank against marinespecies.org...");

            Dictionary<string, WormsRecord> matches;
            try
            {
                matches = WormsService.MatchNamesAsync(unmatchedNames).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log($"WoRMS check: could not reach the WoRMS database ({ex.Message}). Skipping online verification.");
                return;
            }

            int recognized = 0;
            foreach (var name in unmatchedNames)
            {
                if (matches.TryGetValue(name, out var record) && record != null)
                {
                    recognized++;
                    if (record.IsAccepted)
                    {
                        Log($"  [OK]      {name} -- valid marine taxon (AphiaID {record.AphiaID}, {record.Rank}), not present in the reference databank.");
                    }
                    else
                    {
                        Log($"  [SYNONYM] {name} -> accepted name: {record.Valid_name} (AphiaID {record.Valid_AphiaID}, {record.Rank}).");
                    }
                }
                else
                {
                    Log($"  [UNKNOWN] {name} -- not found in the reference databank nor in WoRMS.");
                }
            }

            Log($"WoRMS check complete: {recognized} of {unmatchedNames.Count} unmatched name(s) recognized as valid marine taxa.");
        }

        private static DataTable LoadReferenceDatabank(string listName)
        {
            // Simple mapping based on known files
            // The LoadDataBank class expects the name without extension, and looks in BaseDirectory
            var loader = new LoadDataBank();
            try
            {
                return loader.LoadDataSet(listName);
            }
            catch (FileNotFoundException)
            {
                // Try fuzzy matching if exact name fails, or just return null
                // Known valid names from Form1.cs:
                // jorissen, alve, bouchetmed, bouchetsouthatl, bouchetatl, OMalley2021
                return null;
            }
        }

        private static void ApplyEcoOverridesToDatabank(DataTable refDatabank, Dictionary<string, int> overrides)
        {
            if (refDatabank == null || overrides.Count == 0) return;

            foreach (var kvp in overrides)
            {
                DataRow existing = refDatabank.Rows.Cast<DataRow>()
                    .FirstOrDefault(r => string.Equals(
                        r["Species"]?.ToString()?.Trim(),
                        kvp.Key,
                        StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing["Ecogroup"] = kvp.Value.ToString();
                }
                else
                {
                    DataRow row = refDatabank.NewRow();
                    row["Species"] = kvp.Key;
                    row["Ecogroup"] = kvp.Value.ToString();
                    refDatabank.Rows.Add(row);
                }
            }
        }

        private static Dictionary<string, int> LoadIntegerOverrides(string filePath, string label)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"{label} override file not found: {filePath}");

            var overrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (Path.GetExtension(filePath).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                string json = File.ReadAllText(filePath);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                             ?? new Dictionary<string, int>();

                foreach (var kvp in parsed)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value >= 1 && kvp.Value <= 5)
                        overrides[kvp.Key.Trim()] = kvp.Value;
                }
            }
            else
            {
                foreach (var line in File.ReadLines(filePath).Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Contains(';') ? line.Split(';') : line.Split(',');
                    if (parts.Length >= 2 &&
                        !string.IsNullOrWhiteSpace(parts[0]) &&
                        int.TryParse(parts[1].Trim(), out int value) &&
                        value >= 1 && value <= 5)
                    {
                        overrides[parts[0].Trim()] = value;
                    }
                }
            }

            return overrides;
        }

        private static Dictionary<string, string> LoadCategoryOverrides(
            string filePath,
            string label,
            Func<string, string> normalize)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"{label} override file not found: {filePath}");

            var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (Path.GetExtension(filePath).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                string json = File.ReadAllText(filePath);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                             ?? new Dictionary<string, string>();

                foreach (var kvp in parsed)
                {
                    var value = normalize(kvp.Value);
                    if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(value))
                        overrides[kvp.Key.Trim()] = value;
                }
            }
            else
            {
                foreach (var line in File.ReadLines(filePath).Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Contains(';') ? line.Split(';') : line.Split(',');
                    if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[0]))
                    {
                        var value = normalize(parts[1]);
                        if (!string.IsNullOrWhiteSpace(value))
                            overrides[parts[0].Trim()] = value;
                    }
                }
            }

            return overrides;
        }

        private static string NormalizeFsiCategory(string value)
        {
            string v = value?.Trim().ToUpperInvariant() ?? "";
            return v switch
            {
                "S" or "SEN" or "SENSITIVE" => "S",
                "T" or "STR" or "TOLERANT" or "STRESS" => "T",
                _ => "",
            };
        }

        private static string NormalizeForamCategory(string value)
        {
            string v = value?.Trim().ToUpperInvariant() ?? "";
            return v switch
            {
                "SB" => "SB",
                "ST" => "ST",
                "SH" or "H" => "SH",
                _ => "",
            };
        }

        private static DataTable CalculateIndices(DataTable inputData, DataTable refDatabank, CliOptions options,
            Dictionary<string, double> mudPercentagesPerSample)
        {
            // 1. Determine selected indices
            List<string> selectedIndices = new List<string>();
            string indicesArg = options.Indices;
            string[] availableIndices = new string[]
            {
                "exp(H'bc)", "H'log2", "H'ln", "FSI", "TSI-Med", "NQIf", "FIEI", "Foram-AMBI", "Foram-M-AMBI", "BENTIX", "BQI", "FoRAM Index",
                "Species Richness (S)", "Total Abundance (N)", "Simpson (1-D)", "Pielou's J", "ES100"
            };

            if (string.IsNullOrEmpty(indicesArg) || indicesArg.ToLower() == "all")
            {
                selectedIndices.AddRange(availableIndices);
            }
            else
            {
                var split = indicesArg.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in split)
                {
                    string trim = s.Trim();
                    // Simple case-insensitive match
                    var match = availableIndices.FirstOrDefault(a => a.Equals(trim, StringComparison.OrdinalIgnoreCase));
                    if (match != null) selectedIndices.Add(match);
                }
            }

            // 2. Prepare resources (specialized databanks)
            // Copied logic from AdvancedIndicesForm.LoadResults
            var fsiDatabank = SpecializedDatabankLoader.LoadFSIDatabank();
            var tsiMedDatabank = SpecializedDatabankLoader.LoadTSIMedDatabank();
            var foramDatabank = SpecializedDatabankLoader.LoadFoRAMDatabank();
            if (options.ReplaceFsiWithOverrides && options.FsiOverrides.Count > 0)
                fsiDatabank = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in options.FsiOverrides)
                fsiDatabank[kvp.Key] = kvp.Value;
            foreach (var kvp in options.ForamOverrides)
                foramDatabank[kvp.Key] = kvp.Value;
            var (fsiDatabankAvailable, tsiMedDatabankAvailable) = SpecializedDatabankLoader.CheckDatabanksAvailability();
            var foramDatabankAvailable = SpecializedDatabankLoader.CheckFoRAMDatabankAvailability();
            bool fambiAvailable = (refDatabank != null);
            bool calculateFoRAMIndex = selectedIndices.Contains("FoRAM Index") && foramDatabankAvailable;

            // 3. Prepare results table structure
            DataTable resultsTable = new DataTable("Results");
            resultsTable.Columns.Add("Index", typeof(string));
            for (int col = 1; col < inputData.Columns.Count; col++)
            {
                resultsTable.Columns.Add(inputData.Columns[col].ColumnName, typeof(double));
            }

            // 4. Create EcoGroup lookup
            var ecoLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (fambiAvailable)
            {
                foreach (DataRow row in refDatabank.Rows)
                {
                    string species = row["Species"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(species) && int.TryParse(row["Ecogroup"]?.ToString(), out int eg))
                    {
                        ecoLookup[species] = eg;
                    }
                }
            }

            bool IsSelected(string name) => selectedIndices.Contains(name);
            bool needsEcoGroups = IsSelected("Foram-AMBI") || IsSelected("Foram-M-AMBI") || IsSelected("NQIf") || IsSelected("FIEI") || IsSelected("BENTIX") || IsSelected("BQI") ||
                                  (IsSelected("FSI") && !fsiDatabankAvailable) ||
                                  (IsSelected("TSI-Med") && !tsiMedDatabankAvailable);

            // Store results to populate table later (transposed structure: Index as rows, Samples as cols)
            // We need a map of IndexName -> double[] (values per sample)
            Dictionary<string, double[]> indexValues = new Dictionary<string, double[]>();
            foreach (var idx in selectedIndices)
            {
                indexValues[idx] = new double[inputData.Columns.Count - 1];
            }
            // Also store auxiliary values if needed
            Dictionary<string, double[]> auxValues = new Dictionary<string, double[]>(); 
            // e.g. EcoGroups, FoRAM components, etc. - skipping for basic CLI for now unless requested
            // If requested, I'll add them.

            // 5. Iterate Samples
            int sampleCount = inputData.Columns.Count - 1;
            for (int i = 0; i < sampleCount; i++)
            {
                int colIndex = i + 1; // Column index in inputData
                string sampleName = inputData.Columns[colIndex].ColumnName;

                // Extract abundances, aggregating rows that share the same species name within
                // this sample (summing their abundances) so a taxon entered on multiple rows
                // (e.g. duplicate entries, split size fractions) is counted once, not as several
                // distinct "species" - this matters for richness/diversity as well as eco-groups.
                Dictionary<string, double> speciesAbundances = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                List<double> unnamedAbundances = new List<double>();

                foreach (DataRow row in inputData.Rows)
                {
                    string species = row[0]?.ToString()?.Trim();
                    if (double.TryParse(row[colIndex]?.ToString(), out double value) && value > 0)
                    {
                        if (!string.IsNullOrEmpty(species))
                        {
                            speciesAbundances[species] = speciesAbundances.TryGetValue(species, out double existing)
                                ? existing + value
                                : value;
                        }
                        else
                        {
                            unnamedAbundances.Add(value);
                        }
                    }
                }
                double[] abundArray = speciesAbundances.Values.Concat(unnamedAbundances).ToArray();

                // Calcs
                IndicesResult r = new IndicesResult();
                r.SampleName = sampleName;

                // Diversity
                if (IsSelected("H'ln")) indexValues["H'ln"][i] = BioticIndicesCalculator.CalculateShannonLn(abundArray);
                if (IsSelected("H'log2")) indexValues["H'log2"][i] = BioticIndicesCalculator.CalculateShannonLog2(abundArray);
                if (IsSelected("exp(H'bc)")) indexValues["exp(H'bc)"][i] = BioticIndicesCalculator.CalculateExpHbc(abundArray);
                if (IsSelected("Simpson (1-D)")) 
                {
                     double d = BioticIndicesCalculator.CalculateSimpsonDominance(abundArray);
                     indexValues["Simpson (1-D)"][i] = 1 - d;
                }
                if (IsSelected("Pielou's J")) indexValues["Pielou's J"][i] = BioticIndicesCalculator.CalculatePielousEvenness(abundArray);
                if (IsSelected("ES100")) indexValues["ES100"][i] = BioticIndicesCalculator.CalculateES(abundArray, 100);
                if (IsSelected("Species Richness (S)")) 
                {
                    r.SpeciesRichness = BioticIndicesCalculator.CalculateSpeciesRichness(abundArray);
                    indexValues["Species Richness (S)"][i] = r.SpeciesRichness;
                }
                else { r.SpeciesRichness = BioticIndicesCalculator.CalculateSpeciesRichness(abundArray); } // Calc anyway for others

                if (IsSelected("Total Abundance (N)")) 
                {
                    r.TotalAbundance = BioticIndicesCalculator.CalculateTotalAbundance(abundArray);
                    indexValues["Total Abundance (N)"][i] = r.TotalAbundance;
                }
                else { r.TotalAbundance = BioticIndicesCalculator.CalculateTotalAbundance(abundArray); }

                // EcoGroups
                if (fambiAvailable && needsEcoGroups)
                {
                    r.EcoGroups = BioticIndicesCalculator.CalculateEcoGroupPercentages(speciesAbundances, ecoLookup);
                    r.FAMBI = BioticIndicesCalculator.CalculateFAMBI(r.EcoGroups);
                }
                else
                {
                    r.EcoGroups = new double[] { 0, 0, 0, 0, 0 };
                    r.FAMBI = double.NaN;
                }

                if (IsSelected("Foram-AMBI")) indexValues["Foram-AMBI"][i] = r.FAMBI;

                // FSI
                if (IsSelected("FSI"))
                {
                    if (fsiDatabankAvailable && fsiDatabank.Count > 0)
                    {
                        var (sensitive, tolerant, _) = SpecializedDatabankLoader.CalculateFSIPercentages(speciesAbundances, fsiDatabank);
                        indexValues["FSI"][i] = BioticIndicesCalculator.CalculateFSI(sensitive, tolerant);
                    }
                    else if (fambiAvailable)
                    {
                        double sensitive = r.EcoGroups[0];
                        double tolerant = r.EcoGroups[2] + r.EcoGroups[3] + r.EcoGroups[4];
                        indexValues["FSI"][i] = BioticIndicesCalculator.CalculateFSI(sensitive, tolerant);
                    }
                    else indexValues["FSI"][i] = double.NaN;
                }

                // TSI-Med (uses the per-sample mud row if the input file had one, otherwise -mud=)
                if (IsSelected("TSI-Med"))
                {
                    double mudPct = (mudPercentagesPerSample != null &&
                                     mudPercentagesPerSample.TryGetValue(sampleName, out double sampleMud))
                        ? sampleMud
                        : options.MudPercentage;
                    if (tsiMedDatabankAvailable && tsiMedDatabank.Count > 0)
                    {
                        var (tolerantPct, _) = SpecializedDatabankLoader.CalculateTSIMedPercentages(speciesAbundances, tsiMedDatabank);
                        indexValues["TSI-Med"][i] = BioticIndicesCalculator.CalculateTSIMed(tolerantPct, mudPct);
                    }
                    else if (fambiAvailable)
                    {
                         double tolerant = r.EcoGroups[2] + r.EcoGroups[3] + r.EcoGroups[4];
                         indexValues["TSI-Med"][i] = BioticIndicesCalculator.CalculateTSIMed(tolerant, mudPct);
                    }
                    else indexValues["TSI-Med"][i] = double.NaN;
                }

                // NQIf = 0.5*(1-AMBI/7) + 0.5*(ES100/35) per Alve et al. (2019)
                if (IsSelected("NQIf"))
                {
                     if (fambiAvailable && !double.IsNaN(r.FAMBI))
                     {
                         double es100 = BioticIndicesCalculator.CalculateES(abundArray, 100);
                         indexValues["NQIf"][i] = BioticIndicesCalculator.CalculateNQIf(r.FAMBI, es100);
                     }
                     else indexValues["NQIf"][i] = double.NaN;
                }

                // Foram-M-AMBI
                if (IsSelected("Foram-M-AMBI"))
                {
                    if (fambiAvailable && !double.IsNaN(r.FAMBI))
                    {
                        double h = BioticIndicesCalculator.CalculateShannonLn(abundArray);
                        indexValues["Foram-M-AMBI"][i] = BioticIndicesCalculator.CalculateForamMAMBI(r.FAMBI, h, r.SpeciesRichness);
                    }
                    else indexValues["Foram-M-AMBI"][i] = double.NaN;
                }

                // FIEI
                if (IsSelected("FIEI"))
                {
                    if (fambiAvailable)
                    {
                        double opportunistic = r.EcoGroups[3] + r.EcoGroups[4];
                        double tolerantFIEI = r.EcoGroups[2] + r.EcoGroups[3] + r.EcoGroups[4];
                        indexValues["FIEI"][i] = BioticIndicesCalculator.CalculateFIEI(tolerantFIEI, opportunistic, 100);
                    }
                    else indexValues["FIEI"][i] = double.NaN;
                }

                // BENTIX
                if (IsSelected("BENTIX"))
                {
                    if (fambiAvailable) indexValues["BENTIX"][i] = BioticIndicesCalculator.CalculateBENTIX(r.EcoGroups);
                    else indexValues["BENTIX"][i] = double.NaN;
                }

                // BQI
                if (IsSelected("BQI"))
                {
                     if (fambiAvailable) indexValues["BQI"][i] = BioticIndicesCalculator.CalculateBQI(r.SpeciesRichness, r.EcoGroups, r.TotalAbundance);
                     else indexValues["BQI"][i] = double.NaN;
                }

                // FoRAM Index
                if (calculateFoRAMIndex)
                {
                    var (symbiont, stressTolerant, heterotrophic, assigned) = SpecializedDatabankLoader.CalculateFoRAMPercentages(speciesAbundances, foramDatabank);
                    indexValues["FoRAM Index"][i] = BioticIndicesCalculator.CalculateFoRAMIndex(symbiont, stressTolerant, heterotrophic);

                    // The FoRAM Index is designed for tropical coral-reef assemblages. If only a small
                    // share of this sample's fauna could be classified into a FoRAM functional group,
                    // the computed value is unlikely to be ecologically meaningful.
                    if (assigned < BioticIndicesCalculator.FoRAMIndexMinApplicablePercent)
                    {
                        Log($"Warning: sample '{sampleName}' - only {assigned:F1}% of the assemblage matched FoRAM Index taxa; " +
                            "this index is designed for tropical coral-reef environments and may not be meaningful here.");
                    }
                }
            }

            // 6. Fill DataTable
            foreach (var idx in selectedIndices)
            {
                DataRow row = resultsTable.NewRow();
                row["Index"] = idx;
                double[] vals = indexValues[idx];
                for (int i = 0; i < vals.Length; i++)
                {
                    row[i + 1] = Math.Round(vals[i], 4);
                }
                resultsTable.Rows.Add(row);
            }

            return resultsTable;
        }

        private static void SaveToExcel(DataTable results, string outputPath)
        {
            // Excel (and ClosedXML) cannot represent NaN/Infinity - indices that could not be
            // calculated (e.g. eco-group-based indices when no -list was given) are left as
            // double.NaN. Replace them with a blank cell instead of letting InsertTable throw.
            foreach (DataRow row in results.Rows)
            {
                for (int col = 1; col < results.Columns.Count; col++)
                {
                    if (row[col] is double value && (double.IsNaN(value) || double.IsInfinity(value)))
                    {
                        row[col] = DBNull.Value;
                    }
                }
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Results");
                worksheet.Cell(1, 1).InsertTable(results);
                workbook.SaveAs(outputPath);
            }
        }

        private static void PrintToConsole(DataTable results)
        {
            if (results.Rows.Count == 0) return;

            // Calculate column widths
            int[] columnWidths = new int[results.Columns.Count];
            for (int col = 0; col < results.Columns.Count; col++)
            {
                columnWidths[col] = results.Columns[col].ColumnName.Length;
                foreach (DataRow row in results.Rows)
                {
                    string val = row[col]?.ToString() ?? "";
                    if (val.Length > columnWidths[col]) columnWidths[col] = val.Length;
                }
                columnWidths[col] += 2; // Add padding
            }

            // Print top border
            PrintBorder(columnWidths, "┌", "┬", "┐");

            // Print Header
            Console.Write("│");
            for (int col = 0; col < results.Columns.Count; col++)
            {
                Console.Write(results.Columns[col].ColumnName.PadRight(columnWidths[col]) + "│");
            }
            Console.WriteLine();

            // Print separator
            PrintBorder(columnWidths, "├", "┼", "┤");

            // Print Rows
            foreach (DataRow row in results.Rows)
            {
                Console.Write("│");
                for (int col = 0; col < results.Columns.Count; col++)
                {
                    string val = row[col]?.ToString() ?? "";
                    // Align numbers to the right, index names to the left
                    if (col == 0)
                        Console.Write(val.PadRight(columnWidths[col]) + "│");
                    else
                        Console.Write(val.PadLeft(columnWidths[col]) + "│");
                }
                Console.WriteLine();
            }

            // Print bottom border
            PrintBorder(columnWidths, "└", "┴", "┘");
        }

        private static void PrintBorder(int[] widths, string left, string mid, string right)
        {
            Console.Write(left);
            for (int i = 0; i < widths.Length; i++)
            {
                Console.Write(new string('─', widths[i]));
                if (i < widths.Length - 1) Console.Write(mid);
            }
            Console.WriteLine(right);
        }

        private class CliOptions
        {
            public string InputFile { get; set; }
            public string Indices { get; set; }
            public string ReferenceList { get; set; }
            public string OutputFile { get; set; }
            public double MudPercentage { get; set; } = 50.0;
            public bool UseWorms { get; set; } = false;
            public string EcoOverridesFile { get; set; }
            public string FsiOverridesFile { get; set; }
            public string ForamOverridesFile { get; set; }
            public bool ReplaceFsiWithOverrides { get; set; } = false;
            public Dictionary<string, int> EcoOverrides { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> FsiOverrides { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> ForamOverrides { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

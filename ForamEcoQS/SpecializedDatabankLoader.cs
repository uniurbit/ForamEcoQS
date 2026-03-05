//MIT License
// SpecializedDatabankLoader.cs - Loader for FSI and TSI-Med specialized databanks

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace ForamEcoQS
{
    /// <summary>
    /// Loader for specialized databanks used by FSI and TSI-Med indices
    /// These databanks have different structures than the Foram-AMBI ecogroup databanks
    /// </summary>
    public class SpecializedDatabankLoader
    {
        /// <summary>
        /// Loads FSI databank with Sensitive (S) and Tolerant (T) categories
        /// Based on Dimiza et al. (2016) classification
        /// Checks for user custom databank first, then falls back to original
        /// </summary>
        public static Dictionary<string, string> LoadFSIDatabank()
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            // Check for user custom databank first
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForamEcoQS");
            string userPath = Path.Combine(appDataPath, "fsi_databank_user.csv");
            string originalPath = Path.Combine(baseDirectory, "fsi_databank.csv");
            
            string csvPath = File.Exists(userPath) ? userPath : originalPath;

            if (!File.Exists(csvPath))
            {
                // Return empty dictionary if file not found - FSI will not be calculated
                return lookup;
            }

            try
            {
                using var reader = new StreamReader(csvPath, Encoding.UTF8);
                string? line;
                bool isFirstRow = true;

                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirstRow)
                    {
                        isFirstRow = false;
                        continue;
                    }

                    string[] values;
                    if (line.Contains(';'))
                        values = line.Split(';');
                    else
                        values = line.Split(',');

                    if (values.Length >= 2)
                    {
                        string species = values[0]?.Trim() ?? "";
                        string category = values[1]?.Trim().ToUpper() ?? "";
                        
                        if (!string.IsNullOrEmpty(species) && (category == "S" || category == "T"))
                        {
                            lookup[species] = category;
                            string normalized = SpeciesNameMatcher.NormalizeSpeciesName(species);
                            if (!string.IsNullOrEmpty(normalized) &&
                                !normalized.Equals(species, StringComparison.OrdinalIgnoreCase) &&
                                !lookup.ContainsKey(normalized))
                            {
                                lookup[normalized] = category;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Return empty dictionary on error
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return lookup;
        }

        /// <summary>
        /// Checks if a user custom FSI databank exists
        /// </summary>
        public static bool HasUserFSIDatabank()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForamEcoQS");
            string userPath = Path.Combine(appDataPath, "fsi_databank_user.csv");
            return File.Exists(userPath);
        }

        /// <summary>
        /// Gets info about which FSI databank is currently active
        /// </summary>
        public static (string source, int speciesCount) GetFSIDatabankInfo()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForamEcoQS");
            string userPath = Path.Combine(appDataPath, "fsi_databank_user.csv");
            string originalPath = Path.Combine(baseDirectory, "fsi_databank.csv");

            string activePath = File.Exists(userPath) ? userPath : originalPath;
            string source = File.Exists(userPath) ? "Custom (user)" : "Original";

            if (!File.Exists(activePath))
                return ("Not found", 0);

            int count = 0;
            try
            {
                using var reader = new StreamReader(activePath, Encoding.UTF8);
                string? line;
                bool isFirst = true;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirst) { isFirst = false; continue; }
                    var parts = line.Split(';');
                    if (parts.Length >= 2)
                    {
                        string cat = parts[1]?.Trim().ToUpper() ?? "";
                        if (cat == "S" || cat == "T") count++;
                    }
                }
            }
            catch { }

            return (source, count);
        }

        /// <summary>
        /// Loads TSI-Med databank with Tolerant species list
        /// Based on Barras et al. (2014) classification
        /// </summary>
        public static HashSet<string> LoadTSIMedDatabank()
        {
            var tolerantSpecies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string csvPath = Path.Combine(baseDirectory, "tsimed_databank.csv");

            if (!File.Exists(csvPath))
            {
                return tolerantSpecies;
            }

            try
            {
                using var reader = new StreamReader(csvPath, Encoding.UTF8);
                string? line;
                bool isFirstRow = true;

                while ((line = reader.ReadLine()) != null)
                {
                    if (isFirstRow)
                    {
                        isFirstRow = false;
                        continue;
                    }

                    string[] values = line.Split(';');
                    if (values.Length >= 2)
                    {
                        string species = values[0]?.Trim() ?? "";
                        string category = values[1]?.Trim().ToUpper() ?? "";
                        
                        if (!string.IsNullOrEmpty(species) && category == "T")
                        {
                            tolerantSpecies.Add(species);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return tolerantSpecies;
        }

        /// <summary>
        /// Calculates FSI percentages from species abundances using the specialized FSI databank
        /// </summary>
        /// <param name="speciesAbundances">Dictionary of species names to abundances</param>
        /// <param name="fsiLookup">FSI category lookup (S=Sensitive, T=Tolerant)</param>
        /// <returns>Tuple of (sensitivePercent, tolerantPercent, assignedPercent)</returns>
        public static (double sensitive, double tolerant, double assigned) CalculateFSIPercentages(
            Dictionary<string, double> speciesAbundances,
            Dictionary<string, string> fsiLookup)
        {
            double sensitiveSum = 0;
            double tolerantSum = 0;
            double assignedSum = 0;
            double totalSum = 0;
            var fsiMatcher = new SpeciesNameMatcher(fsiLookup.Keys);

            foreach (var kvp in speciesAbundances)
            {
                string species = kvp.Key.Trim();
                double abundance = kvp.Value;
                totalSum += abundance;

                // Try multiple matching strategies (exact, normalized, genus-level)
                if (TryGetFSICategory(species, fsiLookup, fsiMatcher, out string? category))
                {
                    assignedSum += abundance;
                    if (category == "S")
                        sensitiveSum += abundance;
                    else if (category == "T")
                        tolerantSum += abundance;
                }
            }

            // Calculate percentages based on ASSIGNED species only (as per literature)
            if (assignedSum > 0)
            {
                return (
                    (sensitiveSum / assignedSum) * 100,
                    (tolerantSum / assignedSum) * 100,
                    (assignedSum / totalSum) * 100
                );
            }

            return (0, 0, 0);
        }

        private static bool TryGetFSICategory(
            string speciesName,
            Dictionary<string, string> fsiLookup,
            SpeciesNameMatcher fsiMatcher,
            out string? category)
        {
            // Strategy 1: Direct match (dictionary is case-insensitive)
            if (fsiLookup.TryGetValue(speciesName, out category))
            {
                return true;
            }

            // Strategy 2: Normalized name (remove author citations)
            string normalizedName = SpeciesNameMatcher.NormalizeSpeciesName(speciesName);
            if (!string.IsNullOrEmpty(normalizedName) &&
                !normalizedName.Equals(speciesName, StringComparison.OrdinalIgnoreCase) &&
                fsiLookup.TryGetValue(normalizedName, out category))
            {
                return true;
            }

            // Strategy 3: Intelligent databank comparison
            if (fsiMatcher.TryGetMatchedValue(speciesName, out string matchedValue) &&
                fsiLookup.TryGetValue(matchedValue, out category))
            {
                return true;
            }

            // Strategy 4: Genus-level fallback (first word + "sp.")
            string[] parts = normalizedName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 1)
            {
                string genusMatch = parts[0] + " sp.";
                if (fsiLookup.TryGetValue(genusMatch, out category))
                {
                    return true;
                }
            }

            category = null;
            return false;
        }

        /// <summary>
        /// Calculates TSI-Med tolerant percentage from species abundances using the specialized TSI-Med databank
        /// </summary>
        /// <param name="speciesAbundances">Dictionary of species names to abundances</param>
        /// <param name="tolerantSpecies">Set of tolerant species names</param>
        /// <returns>Tuple of (tolerantPercent, assignedPercent)</returns>
        public static (double tolerant, double assigned) CalculateTSIMedPercentages(
            Dictionary<string, double> speciesAbundances,
            HashSet<string> tolerantSpecies)
        {
            double tolerantSum = 0;
            double totalSum = 0;

            foreach (var kvp in speciesAbundances)
            {
                string species = kvp.Key.Trim();
                double abundance = kvp.Value;
                totalSum += abundance;

                // Try exact match first
                if (tolerantSpecies.Contains(species))
                {
                    tolerantSum += abundance;
                }
                else
                {
                    // Try genus-level match
                    string genus = species.Split(' ')[0] + " sp.";
                    if (tolerantSpecies.Contains(genus))
                    {
                        tolerantSum += abundance;
                    }
                }
            }

            if (totalSum > 0)
            {
                return ((tolerantSum / totalSum) * 100, 100.0);
            }

            return (0, 0);
        }

        /// <summary>
        /// Calculates TSI-Med tolerant percentage using Jorissen et al. (2018) homogenized species list
        /// Tolerant species = EG3 + EG4 + EG5 from the Foram-AMBI databank
        /// Reference: Bouchet et al. (2021) - doi:10.1016/j.marpolbul.2021.112071
        /// </summary>
        /// <param name="speciesAbundances">Dictionary of species names to abundances</param>
        /// <param name="ecoGroupLookup">Ecological group lookup from Foram-AMBI databank</param>
        /// <returns>Percentage of tolerant species (EG3+EG4+EG5)</returns>
        public static double CalculateTSIMedPercentagesJorissen(
            Dictionary<string, double> speciesAbundances,
            Dictionary<string, int> ecoGroupLookup)
        {
            double tolerantSum = 0;
            double assignedSum = 0;
            double totalSum = 0;

            foreach (var kvp in speciesAbundances)
            {
                string species = kvp.Key.Trim();
                double abundance = kvp.Value;
                totalSum += abundance;

                // Try exact match first
                if (ecoGroupLookup.TryGetValue(species, out int ecoGroup))
                {
                    assignedSum += abundance;
                    // EG3, EG4, EG5 are considered tolerant (3rd, 2nd, and 1st order opportunists)
                    if (ecoGroup >= 3)
                    {
                        tolerantSum += abundance;
                    }
                }
                else
                {
                    // Try genus-level match
                    string genus = species.Split(' ')[0] + " sp.";
                    if (ecoGroupLookup.TryGetValue(genus, out ecoGroup))
                    {
                        assignedSum += abundance;
                        if (ecoGroup >= 3)
                        {
                            tolerantSum += abundance;
                        }
                    }
                }
            }

            // Calculate percentage based on total abundance (not just assigned)
            if (totalSum > 0)
            {
                return (tolerantSum / totalSum) * 100;
            }

            return 0;
        }

        /// <summary>
        /// Creates an ecological group lookup dictionary from the Foram-AMBI databank
        /// </summary>
        public static Dictionary<string, int> CreateEcoGroupLookup(DataTable databankTable)
        {
            var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (databankTable == null) return lookup;

            foreach (DataRow row in databankTable.Rows)
            {
                string species = row[0]?.ToString()?.Trim() ?? "";
                string egStr = row[1]?.ToString()?.Trim() ?? "";

                if (!string.IsNullOrEmpty(species) && int.TryParse(egStr, out int eg) && eg >= 1 && eg <= 5)
                {
                    lookup[species] = eg;
                }
            }

            return lookup;
        }

        /// <summary>
        /// Checks if the specialized databanks are available
        /// Also considers user custom databanks
        /// </summary>
        public static (bool fsiAvailable, bool tsiMedAvailable) CheckDatabanksAvailability()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForamEcoQS");
            
            bool fsi = File.Exists(Path.Combine(appDataPath, "fsi_databank_user.csv")) ||
                       File.Exists(Path.Combine(baseDirectory, "fsi_databank.csv"));
            bool tsi = File.Exists(Path.Combine(baseDirectory, "tsimed_databank.csv"));
            return (fsi, tsi);
        }

        /// <summary>
        /// Loads FoRAM Index databank with three functional groups:
        /// SB = Symbiont-Bearing, ST = Stress-Tolerant, SH = Small Heterotrophic
        /// Based on Hallock et al. (2003) and Prazeres et al. (2020)
        /// </summary>
        public static Dictionary<string, string> LoadFoRAMDatabank()
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string csvPath = Path.Combine(baseDirectory, "foram_index_databank.csv");

            if (!File.Exists(csvPath))
            {
                return lookup;
            }

            try
            {
                using var reader = new StreamReader(csvPath, Encoding.UTF8);
                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    // Skip comment lines and empty lines
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    string[] values = line.Split(';');
                    if (values.Length >= 2)
                    {
                        string species = values[0]?.Trim() ?? "";
                        string category = values[1]?.Trim().ToUpper() ?? "";

                        if (!string.IsNullOrEmpty(species) &&
                            (category == "SB" || category == "ST" || category == "SH"))
                        {
                            lookup[species] = category;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return lookup;
        }

        /// <summary>
        /// Calculates FoRAM Index functional group percentages from species abundances
        /// </summary>
        /// <param name="speciesAbundances">Dictionary of species names to abundances</param>
        /// <param name="foramLookup">FoRAM category lookup (SB, ST, SH)</param>
        /// <returns>Tuple of (symbiontBearingPercent, stressTolerantPercent, heterotrophicPercent, assignedPercent)</returns>
        public static (double symbiontBearing, double stressTolerant, double heterotrophic, double assigned)
            CalculateFoRAMPercentages(
                Dictionary<string, double> speciesAbundances,
                Dictionary<string, string> foramLookup)
        {
            double symbiontSum = 0;
            double stressTolerantSum = 0;
            double heterotrophicSum = 0;
            double assignedSum = 0;
            double totalSum = 0;

            foreach (var kvp in speciesAbundances)
            {
                string species = kvp.Key.Trim();
                double abundance = kvp.Value;
                totalSum += abundance;

                // Try exact match first
                if (foramLookup.TryGetValue(species, out string? category))
                {
                    assignedSum += abundance;
                    switch (category)
                    {
                        case "SB":
                            symbiontSum += abundance;
                            break;
                        case "ST":
                            stressTolerantSum += abundance;
                            break;
                        case "SH":
                            heterotrophicSum += abundance;
                            break;
                    }
                }
                else
                {
                    // Try genus-level match (first word + "sp.")
                    string genus = species.Split(' ')[0] + " sp.";
                    if (foramLookup.TryGetValue(genus, out category))
                    {
                        assignedSum += abundance;
                        switch (category)
                        {
                            case "SB":
                                symbiontSum += abundance;
                                break;
                            case "ST":
                                stressTolerantSum += abundance;
                                break;
                            case "SH":
                                heterotrophicSum += abundance;
                                break;
                        }
                    }
                }
            }

            // Calculate percentages based on ASSIGNED species
            if (assignedSum > 0)
            {
                return (
                    (symbiontSum / assignedSum) * 100,
                    (stressTolerantSum / assignedSum) * 100,
                    (heterotrophicSum / assignedSum) * 100,
                    (assignedSum / totalSum) * 100
                );
            }

            return (0, 0, 0, 0);
        }

        /// <summary>
        /// Checks if the FoRAM Index databank is available
        /// </summary>
        public static bool CheckFoRAMDatabankAvailability()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return File.Exists(Path.Combine(baseDirectory, "foram_index_databank.csv"));
        }
    }
}

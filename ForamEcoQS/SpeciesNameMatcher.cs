//MIT License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ForamEcoQS
{
    /// <summary>
    /// Provides intelligent species name matching that handles variations in naming conventions.
    /// Handles cases like "Ammonia parkinsoniana (d' Orbigny, 1839)" matching "Ammonia parkinsoniana".
    /// </summary>
    public class SpeciesNameMatcher
    {
        private readonly HashSet<string> _normalizedDatabankValues;
        private readonly Dictionary<string, string> _originalDatabankValues; // normalized -> original

        public SpeciesNameMatcher(IEnumerable<string> databankValues)
        {
            _normalizedDatabankValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _originalDatabankValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var value in databankValues)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    string normalized = NormalizeSpeciesName(value);
                    if (!_normalizedDatabankValues.Contains(normalized))
                    {
                        _normalizedDatabankValues.Add(normalized);
                        _originalDatabankValues[normalized] = value;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a species name from the grid matches any entry in the databank.
        /// Uses intelligent matching that handles author citations and variations.
        /// </summary>
        public bool IsMatch(string gridValue)
        {
            if (string.IsNullOrWhiteSpace(gridValue))
                return false;

            string normalizedGridValue = NormalizeSpeciesName(gridValue);

            // 1. Direct match after normalization
            if (_normalizedDatabankValues.Contains(normalizedGridValue))
                return true;

            // 2. Check if any databank entry is contained within the grid value
            //    (handles case where grid has "Genus species author" and databank has "Genus species")
            foreach (var databankValue in _normalizedDatabankValues)
            {
                if (normalizedGridValue.StartsWith(databankValue, StringComparison.OrdinalIgnoreCase))
                {
                    // Ensure it's a word boundary match (not partial word)
                    if (normalizedGridValue.Length == databankValue.Length ||
                        !char.IsLetterOrDigit(normalizedGridValue[databankValue.Length]))
                    {
                        return true;
                    }
                }
            }

            // 3. Check if the grid value is contained in any databank entry
            //    (handles case where grid has "Genus species" and databank has "Genus species var. x")
            foreach (var databankValue in _normalizedDatabankValues)
            {
                if (databankValue.StartsWith(normalizedGridValue, StringComparison.OrdinalIgnoreCase))
                {
                    if (databankValue.Length == normalizedGridValue.Length ||
                        !char.IsLetterOrDigit(databankValue[normalizedGridValue.Length]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to find a matching databank entry for a species name and returns the original value.
        /// </summary>
        public bool TryGetMatchedValue(string gridValue, out string matchedValue)
        {
            matchedValue = string.Empty;
            if (string.IsNullOrWhiteSpace(gridValue))
                return false;

            string normalizedGridValue = NormalizeSpeciesName(gridValue);

            if (_normalizedDatabankValues.Contains(normalizedGridValue) &&
                _originalDatabankValues.TryGetValue(normalizedGridValue, out matchedValue))
            {
                return true;
            }

            foreach (var databankValue in _normalizedDatabankValues)
            {
                if (normalizedGridValue.StartsWith(databankValue, StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedGridValue.Length == databankValue.Length ||
                        !char.IsLetterOrDigit(normalizedGridValue[databankValue.Length]))
                    {
                        if (_originalDatabankValues.TryGetValue(databankValue, out matchedValue))
                        {
                            return true;
                        }
                    }
                }
            }

            foreach (var databankValue in _normalizedDatabankValues)
            {
                if (databankValue.StartsWith(normalizedGridValue, StringComparison.OrdinalIgnoreCase))
                {
                    if (databankValue.Length == normalizedGridValue.Length ||
                        !char.IsLetterOrDigit(databankValue[normalizedGridValue.Length]))
                    {
                        if (_originalDatabankValues.TryGetValue(databankValue, out matchedValue))
                        {
                            return true;
                        }
                    }
                }
            }

            matchedValue = string.Empty;
            return false;
        }

        /// <summary>
        /// Normalizes a species name for comparison by:
        /// - Removing content in parentheses (author citations like "(d'Orbigny, 1839)")
        /// - Removing extra whitespace
        /// - Trimming
        /// </summary>
        public static string NormalizeSpeciesName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Remove content in parentheses (author citations)
            // Handles nested parentheses and multiple parenthetical groups
            string result = Regex.Replace(name, @"\s*\([^)]*\)", "");

            // Remove content in square brackets
            result = Regex.Replace(result, @"\s*\[[^\]]*\]", "");

            // Normalize whitespace (multiple spaces to single space)
            result = Regex.Replace(result, @"\s+", " ");

            // Trim leading/trailing whitespace
            result = result.Trim();

            // Remove trailing punctuation that might remain
            result = result.TrimEnd(',', ';', '.');

            return result;
        }

        /// <summary>
        /// Gets the count of entries in the databank.
        /// </summary>
        public int DatabankCount => _normalizedDatabankValues.Count;
    }
}

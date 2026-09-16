//MIT License
// SampleMatrixLayout.cs - Detects whether a loaded abundance matrix is the right way round,
// and transposes it when it is not.
//
// ForamEcoQS expects species down the first column and one sample per following column.
// Spreadsheets in the wild are just as often the other way round (samples in rows, species
// across the header). Rather than silently mis-reading such a file as "one species called
// Station 1 with N samples", the loaders run this detector and flip the table when needed.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ForamEcoQS
{
    /// <summary>Which axis of the matrix carries the species names.</summary>
    public enum MatrixOrientation
    {
        /// <summary>Species in the first column, samples across the header. The expected layout.</summary>
        SpeciesInRows,

        /// <summary>Samples in the first column, species across the header. Needs transposing.</summary>
        SpeciesInColumns,

        /// <summary>Neither axis looks clearly like species names.</summary>
        Unknown
    }

    /// <summary>Outcome of a layout check.</summary>
    public sealed class MatrixLayoutResult
    {
        public MatrixOrientation Orientation { get; init; }

        /// <summary>0..1; how much more species-like the winning axis is than the other.</summary>
        public double Confidence { get; init; }

        /// <summary>Short human readable explanation, shown to the user or logged by the CLI.</summary>
        public string Reason { get; init; } = string.Empty;

        /// <summary>True when the table should be transposed before use.</summary>
        public bool NeedsTranspose => Orientation == MatrixOrientation.SpeciesInColumns;
    }

    public static class SampleMatrixLayout
    {
        /// <summary>Above this, the layout is flipped without asking.</summary>
        public const double HighConfidence = 0.30;

        /// <summary>Between this and <see cref="HighConfidence"/>, the caller should ask the user.</summary>
        public const double AmbiguousConfidence = 0.10;

        // "Ammonia beccarii", "Elphidium cf. excavatum", "Quinqueloculina sp."
        private static readonly Regex BinomialPattern = new Regex(
            @"^[A-Z][a-z]{2,}\s+((cf\.|aff\.|ex\s+gr\.)\s+)?([a-z][a-z\-]{2,}|sp{1,2}\.|indet\.)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // "Sample 1", "S3", "St. 12", "Station_A", "Core 4", "Site-7"
        private static readonly Regex SampleCodePattern = new Regex(
            @"^(s|st|sta|site|station|sample|samp|camp|campione|stazione|core|carota|tr|transect|pt|point)\s*[-_.]?\s*\d+[a-z]?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly string[] SpeciesHeaderWords =
        {
            "species", "specie", "taxon", "taxa", "taxonomy", "taxon name", "scientific name", "nome"
        };

        private static readonly string[] SampleHeaderWords =
        {
            "sample", "samples", "station", "stations", "site", "sites",
            "campione", "campioni", "stazione", "stazioni", "core", "carota", "id"
        };

        private static readonly string[] MudWords =
        {
            "mud", "mud (%)", "mud(%)", "% mud", "%mud", "fango", "fango (%)"
        };

        /// <summary>
        /// Decides which way round the matrix is. The check is deliberately conservative: it
        /// only reports <see cref="MatrixOrientation.SpeciesInColumns"/> when the header row
        /// looks more like a species list than the first column does.
        /// </summary>
        public static MatrixLayoutResult Detect(DataTable table)
        {
            if (table == null || table.Columns.Count < 2 || table.Rows.Count == 0)
            {
                return new MatrixLayoutResult
                {
                    Orientation = MatrixOrientation.Unknown,
                    Confidence = 0,
                    Reason = "The table is too small to determine its layout."
                };
            }

            string corner = table.Columns[0].ColumnName?.Trim() ?? string.Empty;

            var headerNames = table.Columns.Cast<DataColumn>()
                .Skip(1)
                .Select(c => c.ColumnName?.Trim() ?? string.Empty)
                .ToList();

            var firstColumnValues = table.Rows.Cast<DataRow>()
                .Select(r => r[0]?.ToString()?.Trim() ?? string.Empty)
                .ToList();

            // 1. An explicit corner label is the strongest signal there is.
            if (MatchesAny(corner, SpeciesHeaderWords))
            {
                return new MatrixLayoutResult
                {
                    Orientation = MatrixOrientation.SpeciesInRows,
                    Confidence = 1.0,
                    Reason = $"The first column is labelled '{corner}'."
                };
            }

            if (MatchesAny(corner, SampleHeaderWords))
            {
                return new MatrixLayoutResult
                {
                    Orientation = MatrixOrientation.SpeciesInColumns,
                    Confidence = 1.0,
                    Reason = $"The first column is labelled '{corner}', so the rows are samples."
                };
            }

            // 2. A mud (%) entry sits on the sample axis, so it tells us where the samples are.
            bool mudInFirstColumn = firstColumnValues.Any(v => MatchesAny(v, MudWords));
            bool mudInHeader = headerNames.Any(v => MatchesAny(v, MudWords));

            if (mudInFirstColumn && !mudInHeader)
            {
                return new MatrixLayoutResult
                {
                    Orientation = MatrixOrientation.SpeciesInRows,
                    Confidence = 0.9,
                    Reason = "A mud (%) row was found, so the columns are samples."
                };
            }

            if (mudInHeader && !mudInFirstColumn)
            {
                return new MatrixLayoutResult
                {
                    Orientation = MatrixOrientation.SpeciesInColumns,
                    Confidence = 0.9,
                    Reason = "A mud (%) column was found, so the rows are samples."
                };
            }

            // 3. Otherwise score both axes on how much they read like a species list.
            double rowScore = SpeciesLikelihood(firstColumnValues);
            double columnScore = SpeciesLikelihood(headerNames);
            double difference = Math.Abs(rowScore - columnScore);

            if (difference < AmbiguousConfidence)
            {
                return new MatrixLayoutResult
                {
                    Orientation = MatrixOrientation.Unknown,
                    Confidence = difference,
                    Reason = "Neither axis clearly holds species names " +
                             $"(rows {rowScore:P0}, columns {columnScore:P0})."
                };
            }

            if (columnScore > rowScore)
            {
                return new MatrixLayoutResult
                {
                    Orientation = MatrixOrientation.SpeciesInColumns,
                    Confidence = difference,
                    Reason = $"{columnScore:P0} of the column headers look like species names, " +
                             $"against {rowScore:P0} of the first column."
                };
            }

            return new MatrixLayoutResult
            {
                Orientation = MatrixOrientation.SpeciesInRows,
                Confidence = difference,
                Reason = $"{rowScore:P0} of the first column looks like species names, " +
                         $"against {columnScore:P0} of the column headers."
            };
        }

        /// <summary>
        /// Returns a table with species in rows, transposing the input when the detector says so.
        /// </summary>
        /// <param name="table">The loaded table.</param>
        /// <param name="result">What the detector concluded.</param>
        /// <param name="force">
        /// Transpose regardless of detection (true) or never transpose (false). Null auto-detects.
        /// </param>
        public static DataTable EnsureSpeciesInRows(DataTable table, out MatrixLayoutResult result, bool? force = null)
        {
            result = Detect(table);

            bool shouldTranspose = force ?? (result.NeedsTranspose && result.Confidence >= HighConfidence);

            return shouldTranspose ? Transpose(table) : table;
        }

        /// <summary>
        /// Flips a matrix around its corner cell: the header row becomes the first column and
        /// vice versa. Column names are made unique, because Excel headers often repeat.
        /// </summary>
        public static DataTable Transpose(DataTable table)
        {
            if (table == null)
            {
                return null;
            }

            var transposed = new DataTable(table.TableName);

            // The corner labels whatever now runs down the first column, so it has to swap sides
            // with the data: keeping "Species" above a column of station names would misdescribe
            // the table and make the detector read the flipped result the wrong way round.
            transposed.Columns.Add(FlipCornerLabel(table.Columns[0].ColumnName), typeof(string));

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                transposed.Columns[0].ColumnName
            };

            foreach (DataRow row in table.Rows)
            {
                string name = MakeUnique(NonEmpty(row[0]?.ToString(), "Sample"), usedNames);
                transposed.Columns.Add(name, typeof(string));
            }

            for (int col = 1; col < table.Columns.Count; col++)
            {
                var newRow = transposed.NewRow();
                newRow[0] = table.Columns[col].ColumnName;

                for (int row = 0; row < table.Rows.Count; row++)
                {
                    object value = table.Rows[row][col];
                    newRow[row + 1] = value == null || value == DBNull.Value
                        ? string.Empty
                        : Convert.ToString(value, CultureInfo.CurrentCulture);
                }

                transposed.Rows.Add(newRow);
            }

            return transposed;
        }

        /// <summary>
        /// Fraction of the given labels that read like a taxon name rather than a sample code.
        /// </summary>
        public static double SpeciesLikelihood(IReadOnlyCollection<string> labels)
        {
            if (labels == null || labels.Count == 0)
            {
                return 0;
            }

            var candidates = labels
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Where(l => !MatchesAny(l, MudWords))
                .ToList();

            if (candidates.Count == 0)
            {
                return 0;
            }

            double score = 0;

            foreach (string label in candidates)
            {
                string value = label.Trim();

                // Excel gives unnamed columns names like "Column3"; they carry no information.
                if (SampleCodePattern.IsMatch(value) || IsNumeric(value) || value.StartsWith("Column", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (BinomialPattern.IsMatch(value))
                {
                    score += 1.0;
                    continue;
                }

                // Weaker signals: an author citation, or a capitalised multi-word label.
                bool hasAuthorCitation = value.Contains('(') && Regex.IsMatch(value, @"\(\s*[A-Za-z' ]+,?\s*\d{4}\s*\)");
                bool capitalisedPhrase = value.Length > 5 && char.IsUpper(value[0]) && value.Contains(' ');

                if (hasAuthorCitation)
                {
                    score += 0.9;
                }
                else if (capitalisedPhrase)
                {
                    score += 0.4;
                }
            }

            return score / candidates.Count;
        }

        private static bool MatchesAny(string value, IEnumerable<string> words)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = value.Trim().Trim(':').Trim();
            return words.Any(w => string.Equals(normalized, w, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsNumeric(string value) =>
            double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _) ||
            double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out _);

        // Excel names unheaded columns "Column1", "Column2", ...
        private static readonly Regex PlaceholderColumnPattern = new Regex(
            @"^Column\s*\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Turns the corner label of the source table into the corner label of the transposed one.
        /// A blank corner arrives as Excel's placeholder ("Column1") and is treated as unlabelled.
        /// </summary>
        private static string FlipCornerLabel(string value)
        {
            string corner = value?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(corner) || PlaceholderColumnPattern.IsMatch(corner))
            {
                return "Species";
            }

            if (MatchesAny(corner, SpeciesHeaderWords))
            {
                return "Sample";
            }

            if (MatchesAny(corner, SampleHeaderWords))
            {
                return "Species";
            }

            return corner;
        }

        private static string NonEmpty(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        private static string MakeUnique(string name, HashSet<string> used)
        {
            if (used.Add(name))
            {
                return name;
            }

            for (int i = 2; ; i++)
            {
                string candidate = $"{name}_{i}";
                if (used.Add(candidate))
                {
                    return candidate;
                }
            }
        }
    }
}

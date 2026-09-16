//MIT License
// ExcelDataLoader.cs - Platform independent replacement for the WinForms-only ExcelReader.
// The UI layer supplies a progress callback; no UI types are referenced here so the same
// loader is shared by the Eto.Forms front-end and the command line runner.

using System;
using System.Data;
using System.IO;
using System.Text;
using ExcelDataReader;

namespace ForamEcoQS
{
    /// <summary>
    /// Reports loading progress back to whatever front-end is driving the load.
    /// </summary>
    /// <param name="status">Human readable status message.</param>
    /// <param name="percent">Progress in percent, or -1 when indeterminate.</param>
    public delegate void LoadProgressCallback(string status, int percent);

    public static class ExcelDataLoader
    {
        private static bool _encodingProviderRegistered;

        /// <summary>
        /// ExcelDataReader needs the Windows code pages for legacy .xls files. On .NET 5+ these
        /// are not registered by default on any platform, so make sure it happens exactly once.
        /// </summary>
        public static void EnsureEncodingProvider()
        {
            if (_encodingProviderRegistered)
            {
                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _encodingProviderRegistered = true;
        }

        /// <summary>
        /// Reads the first worksheet of an Excel file (.xls or .xlsx) into a DataTable,
        /// using the first row as the header row.
        /// </summary>
        public static DataTable ReadExcelFile(string filePath)
        {
            EnsureEncodingProvider();

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = true
                    }
                });

                return result.Tables.Count > 0 ? result.Tables[0] : null;
            }
        }

        /// <summary>
        /// Loads an Excel file and removes the trailing all-empty columns that spreadsheet
        /// applications routinely leave behind.
        /// </summary>
        public static DataTable LoadTrimmed(string filePath, LoadProgressCallback progress = null)
        {
            progress?.Invoke("Parsing Excel data...", -1);
            DataTable dataTable = ReadExcelFile(filePath);
            if (dataTable == null)
            {
                return null;
            }

            progress?.Invoke("Removing empty columns...", 30);
            return TrimEmptyColumns(dataTable, progress);
        }

        /// <summary>
        /// Removes every column after the last one that holds data.
        /// </summary>
        public static DataTable TrimEmptyColumns(DataTable dataTable, LoadProgressCallback progress = null)
        {
            int lastNonEmptyColumnIndex = -1;
            int totalCols = dataTable.Columns.Count;
            if (totalCols == 0)
            {
                return dataTable;
            }

            for (int colIndex = totalCols - 1; colIndex >= 0; colIndex--)
            {
                bool hasData = false;
                foreach (DataRow row in dataTable.Rows)
                {
                    var value = row[colIndex];
                    if (value != null && value != DBNull.Value && !string.IsNullOrWhiteSpace(value.ToString()))
                    {
                        hasData = true;
                        break;
                    }
                }

                if (hasData)
                {
                    lastNonEmptyColumnIndex = colIndex;
                    break;
                }

                int scanned = totalCols - colIndex;
                if (scanned % 50 == 0)
                {
                    progress?.Invoke($"Checking columns... ({scanned}/{totalCols})", 30 + (scanned * 40 / totalCols));
                }
            }

            if (lastNonEmptyColumnIndex < 0)
            {
                dataTable.Columns.Clear();
                return dataTable;
            }

            int columnsToRemove = dataTable.Columns.Count - lastNonEmptyColumnIndex - 1;
            if (columnsToRemove > 0)
            {
                progress?.Invoke($"Removing {columnsToRemove} empty columns...", 70);
                for (int i = dataTable.Columns.Count - 1; i > lastNonEmptyColumnIndex; i--)
                {
                    dataTable.Columns.RemoveAt(i);
                }
            }

            return dataTable;
        }

        /// <summary>
        /// Loads a delimited text file into a DataTable. The first column is kept as text and
        /// every other column is typed as double, matching how the sample grid expects data.
        /// </summary>
        public static DataTable ReadCsvFile(string filePath, char separator)
        {
            var dataTable = new DataTable();

            using (var reader = new StreamReader(filePath))
            {
                bool isFirstRow = true;
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(separator);

                    if (isFirstRow)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            dataTable.Columns.Add(values[i], i == 0 ? typeof(string) : typeof(double));
                        }
                        isFirstRow = false;
                        continue;
                    }

                    DataRow dataRow = dataTable.NewRow();
                    int columnCount = Math.Min(values.Length, dataTable.Columns.Count);
                    for (int i = 0; i < columnCount; i++)
                    {
                        if (i == 0)
                        {
                            dataRow[i] = values[i];
                        }
                        else if (double.TryParse(values[i], out double numericValue))
                        {
                            dataRow[i] = numericValue;
                        }
                        else
                        {
                            dataRow[i] = DBNull.Value;
                        }
                    }

                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
        }
    }
}

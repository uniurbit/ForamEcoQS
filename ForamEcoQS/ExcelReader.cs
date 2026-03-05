//MIT License

using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ExcelDataReader;
using ForamEcoQS;

public class ExcelReader
{
    public void LoadExcelIntoDataGridView(DataGridView dataGridView, string filePath)
    {
        // Create and show loading form
        var loadingForm = new LoadingForm();
        loadingForm.Show(dataGridView.FindForm());
        loadingForm.SetStatus("Reading Excel file...");
        Application.DoEvents();

        try
        {
            // Register the code page provider
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            dataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dataGridView.DataSource = null;
            dataGridView.Columns.Clear();

            // Read the Excel file and load it into a DataTable
            loadingForm.SetStatus("Parsing Excel data...");
            DataTable dataTable = ReadExcelFile(filePath);

            // Trim empty columns
            loadingForm.SetStatus("Removing empty columns...");
            Application.DoEvents();
            dataTable = TrimEmptyColumns(dataTable, loadingForm);

            loadingForm.SetStatus($"Loading {dataTable.Columns.Count} columns into grid...");
            loadingForm.SetProgress(70);

            // Disable auto-generation of columns to avoid FillWeight overflow
            // (default FillWeight is 100 per column, max total is 65535)
            dataGridView.AutoGenerateColumns = false;

            // Manually create columns with minimal FillWeight to avoid overflow
            int colIndex = 0;
            int totalCols = dataTable.Columns.Count;
            foreach (DataColumn col in dataTable.Columns)
            {
                var dgvCol = new DataGridViewTextBoxColumn
                {
                    DataPropertyName = col.ColumnName,
                    Name = col.ColumnName,
                    HeaderText = col.ColumnName,
                    ValueType = col.DataType,
                    FillWeight = 1 // Use minimal FillWeight to prevent overflow
                };
                dataGridView.Columns.Add(dgvCol);
                colIndex++;
                if (colIndex % 10 == 0)
                {
                    loadingForm.SetProgress(70 + (colIndex * 20 / totalCols));
                }
            }

            loadingForm.SetStatus("Binding data...");
            loadingForm.SetProgress(90);

            // Set the DataGridView's DataSource to the DataTable
            dataGridView.DataSource = dataTable;
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                if (column.ValueType == typeof(double) || column.ValueType == typeof(float) || column.ValueType == typeof(decimal))
                {
                    column.DefaultCellStyle.Format = "N2";
                }
            }
            // Set the SortMode of each column to NotSortable
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            loadingForm.SetProgress(100);
            loadingForm.SetStatus("Complete!");
        }
        finally
        {
            loadingForm.Close();
            loadingForm.Dispose();
        }
    }

    private DataTable TrimEmptyColumns(DataTable dataTable, LoadingForm loadingForm)
    {
        // Find the last non-empty column
        int lastNonEmptyColumnIndex = -1;
        int totalCols = dataTable.Columns.Count;

        for (int colIndex = dataTable.Columns.Count - 1; colIndex >= 0; colIndex--)
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

            // Update progress (searching from end, so reverse the progress)
            int progress = 30 + ((totalCols - colIndex) * 40 / totalCols);
            if ((totalCols - colIndex) % 50 == 0)
            {
                loadingForm.SetProgress(progress);
                loadingForm.SetStatus($"Checking columns... ({totalCols - colIndex}/{totalCols})");
            }
        }

        // If all columns are empty or we need to trim some
        if (lastNonEmptyColumnIndex < 0)
        {
            // Return empty table with no columns
            dataTable.Columns.Clear();
            return dataTable;
        }

        // Remove columns after the last non-empty one
        int columnsToRemove = dataTable.Columns.Count - lastNonEmptyColumnIndex - 1;
        if (columnsToRemove > 0)
        {
            loadingForm.SetStatus($"Removing {columnsToRemove} empty columns...");
            Application.DoEvents();

            for (int i = dataTable.Columns.Count - 1; i > lastNonEmptyColumnIndex; i--)
            {
                dataTable.Columns.RemoveAt(i);
            }
        }

        return dataTable;
    }

    private DataTable ReadExcelFile(string filePath)
    {
        // Initialize the stream and reader for the Excel file
        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
        {
            // Initialize the reader to read the Excel file
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                // Use AsDataSet to get all data from the Excel file
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true // Set this to true if the first row contains column names
                    }
                });

                // Return the first DataTable from the DataSet (assuming there's only one worksheet)
                return result.Tables[0];
            }
        }
    }
}

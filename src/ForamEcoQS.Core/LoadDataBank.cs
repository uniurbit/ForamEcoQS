//MIT License

using System;
using System.Data;
using System.IO;
using System.Text;
using ExcelDataReader;

public class LoadDataBank
{
    public DataTable LoadDataSet(string datasetName)
    {
        DataTable databank = CreateDataBankTable();

        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string csvPath = Path.Combine(baseDirectory, $"{datasetName}.csv");
        string xlsPath = Path.Combine(baseDirectory, $"{datasetName}.xls");

        if (File.Exists(csvPath))
        {
            LoadFromCsv(csvPath, databank);
        }
        else if (File.Exists(xlsPath))
        {
            LoadFromExcel(xlsPath, databank);
            TryWriteCsv(csvPath, databank);
        }
        else
        {
            throw new FileNotFoundException($"The file {datasetName}.csv or {datasetName}.xls was not found.");
        }

        return databank;
    }

    private static DataTable CreateDataBankTable()
    {
        var databank = new DataTable();
        databank.Columns.Add("Species", typeof(string));
        databank.Columns.Add("Ecogroup", typeof(string));
        return databank;
    }

    private static void LoadFromCsv(string filePath, DataTable databank)
    {
        using var reader = new StreamReader(filePath, Encoding.UTF8);

        string? line;
        bool isFirstRow = true;

        while ((line = reader.ReadLine()) != null)
        {
            string[] values = line.Split(';');

            if (isFirstRow)
            {
                isFirstRow = false;
                continue;
            }

            if (values.Length >= 2)
            {
                DataRow row = databank.NewRow();
                row["Species"] = values[0];
                row["Ecogroup"] = values[1];
                databank.Rows.Add(row);
            }
        }
    }

    private static void LoadFromExcel(string filePath, DataTable databank)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var result = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = (_) => new ExcelDataTableConfiguration
            {
                UseHeaderRow = true
            }
        });

        if (result.Tables.Count == 0)
        {
            return;
        }

        DataTable table = result.Tables[0];

        foreach (DataRow sourceRow in table.Rows)
        {
            if (table.Columns.Count < 2)
            {
                continue;
            }

            DataRow newRow = databank.NewRow();
            newRow["Species"] = sourceRow[0]?.ToString();
            newRow["Ecogroup"] = sourceRow[1]?.ToString();
            databank.Rows.Add(newRow);
        }
    }

    private static void TryWriteCsv(string destinationPath, DataTable databank)
    {
        try
        {
            using var writer = new StreamWriter(destinationPath, false, Encoding.UTF8);
            writer.WriteLine("Species;Ecogroup");

            foreach (DataRow row in databank.Rows)
            {
                writer.WriteLine($"{row["Species"]};{row["Ecogroup"]}");
            }
        }
        catch (IOException)
        {
            // If we cannot persist the CSV, we still allow the application to continue
        }
    }
}
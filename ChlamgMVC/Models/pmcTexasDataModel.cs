using Microsoft.Data.SqlClient;
using Serilog;
using System.Data;

namespace ChlamgMVC.Models
{
    public class PmctexasDataModel
    {
        private readonly string _connectionString;

        public PmctexasDataModel(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void LoadExcelAndInsertData(string excelFilePath)
        {
           // TruncateTables();
            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            Log.Information("Loading Excel file from path: {ExcelFilePath}", excelFilePath);
            DataTable dataTable = LoadExcelFileToDataTable(excelFilePath);

            Log.Information("Inserting data into SQL Server...");
            BulkInsertDataTableToSqlServer(dataTable);
            Log.Information("Data inserted successfully.");

            // Additional Pmctexas-specific methods...
        }

        private void BulkInsertDataTableToSqlServer(DataTable dataTable)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "dbo.tbl_TempPMCTexas_Invenotry"; // Correct table
                                                                                      // Add column mappings for Pmctexas...

                    bulkCopy.ColumnMappings.Add("Rendering Provider", "Rendering Provider");
                    bulkCopy.ColumnMappings.Add("Payer Name", "Payer Name");
                    bulkCopy.ColumnMappings.Add("Payer Address Line 1", "Payer Address Line 1");
                    bulkCopy.ColumnMappings.Add("Payer Address Line 2", "Payer Address Line 2");
                    bulkCopy.ColumnMappings.Add("Payer City", "Payer City");
                    bulkCopy.ColumnMappings.Add("Payer State", "Payer State");
                    bulkCopy.ColumnMappings.Add("Payer ZIP Code", "Payer ZIP Code");
                    bulkCopy.ColumnMappings.Add("Payer Phone No", "Payer Phone No");
                    bulkCopy.ColumnMappings.Add("Patient Name", "Patient Name");
                    bulkCopy.ColumnMappings.Add("Patient Acct No", "Patient Acct No");
                    bulkCopy.ColumnMappings.Add("Patient DOB", "Patient DOB");
                    bulkCopy.ColumnMappings.Add("Payer Group No", "Payer Group No");
                    bulkCopy.ColumnMappings.Add("Payer Subscriber No", "Payer Subscriber No");
                    bulkCopy.ColumnMappings.Add("Aging Days", "Aging Days");
                    bulkCopy.ColumnMappings.Add("Claim Date", "Claim Date");
                    bulkCopy.ColumnMappings.Add("Service Date", "Service Date");
                    bulkCopy.ColumnMappings.Add("Latest Transfer Date", "Latest Transfer Date");
                    bulkCopy.ColumnMappings.Add("Last Submission Date", "Last Submission Date");
                    bulkCopy.ColumnMappings.Add("Last Claim Status Change Date", "Last Claim Status Change Date");
                    bulkCopy.ColumnMappings.Add("Claim No", "Claim No");
                    bulkCopy.ColumnMappings.Add("Charges", "Charges");
                    bulkCopy.ColumnMappings.Add("Claims Not Submitted", "Claims Not Submitted");
                    bulkCopy.ColumnMappings.Add("Current", "Current");
                    bulkCopy.ColumnMappings.Add("31-60", "31-60");
                    bulkCopy.ColumnMappings.Add("61-90", "61-90");
                    bulkCopy.ColumnMappings.Add("91-120", "91-120");
                    bulkCopy.ColumnMappings.Add("> 120", "> 120");
                    bulkCopy.ColumnMappings.Add("Balance", "Balance");
                    bulkCopy.WriteToServer(dataTable);
                }
            }
        }

        private DataTable LoadExcelFileToDataTable(string excelFilePath)
        {
            var dataTable = new DataTable();

            using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(excelFilePath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Load the first worksheet
                var rowCount = worksheet.Dimension.Rows;
                var colCount = worksheet.Dimension.Columns;

                // Add column names to the DataTable
                for (int col = 1; col <= colCount; col++)
                {
                    dataTable.Columns.Add(worksheet.Cells[1, col].Text); // Assuming the first row contains column headers
                }

                // Add rows to the DataTable
                for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip headers
                {
                    var newRow = dataTable.NewRow();
                    for (int col = 1; col <= colCount; col++)
                    {
                        newRow[col - 1] = worksheet.Cells[row, col].Text; // Fill the row
                    }
                    dataTable.Rows.Add(newRow);
                }
            }

            return dataTable; // Ensure you return the populated DataTable
        }

        // Other specific methods for Pmctexas...
    }

}

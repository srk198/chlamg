using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using OfficeOpenXml; // Make sure to install EPPlus NuGet package
using Serilog; // Assuming you're using Serilog for logging
using Microsoft.Data.SqlClient; // Use this for SQL Server commands
using System.Reflection;
using Serilog;

namespace ChlamgMVC.Models
{
    public class DataModel
    {

        private readonly string _connectionString;

        // Constructor to initialize the connection string
        public DataModel(string connectionString)
        {
            _connectionString = connectionString;
        }

      //  Method to load data from Excel and insert into SQL Server
        public void LoadExcelAndInsertData(string excelFilePath)
        {
            var methodNames = typeof(Program)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Where(m => m.DeclaringType == typeof(Program))
                .Select(m => m.Name);

            Console.WriteLine("Methods in Program class:");
            foreach (var name in methodNames)
            {
                Console.WriteLine(name);
            }

            try
            {

                // Truncate tables before inserting new data
                TruncateTables();
                // Set the license context for EPPlus
                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial; // Fully qualified name


                Log.Information("Loading Excel file from path: {ExcelFilePath}", excelFilePath);

                // Load data from Excel into DataTable
                DataTable dataTable = LoadExcelFileToDataTable(excelFilePath);
                Log.Information("Excel file loaded successfully.");
                foreach (DataRow row in dataTable.Rows)
                {
                    foreach (var item in row.ItemArray)
                    {
                        Console.Write($"{item}\t"); // Print each item in the row
                    }
                    Console.WriteLine(); // New line after each row
                }
                Console.WriteLine("DataTable Columns:");
                foreach (DataColumn column in dataTable.Columns)
                {
                    Console.WriteLine(column.ColumnName);
                }

                Log.Information("Inserting data into SQL Server...");
                //Bulk insert the data into SQL Server
                BulkInsertDataTableToSqlServer(dataTable);

                Log.Information("Data inserted successfully.");

                // Perform additional operations
                InputFieldsSelectedFields(_connectionString);
                UpdateInventoryStagingTable();

                //  -----------------------------------------------------------------------------Lookup from here
                DeleteNullClaimNumbersFromInventoryStaging(_connectionString);

                // Handle duplicates
                int fileID = 1234;  // Example FileID
                HandleDuplicatesInInventoryStaging(_connectionString, fileID);
                RemoveDuplicatesAndInsertIntoDuplicatesTable(_connectionString, fileID);
                SequentialInsertProcess(_connectionString, "tbl_Inventory_CHLAMG", " [UID_Account_Number],\r\n        [Claim_Number],\r\n        [Exclusions],\r\n        [Financial_Class],\r\n        [Date_of_Service],\r\n        [File_Creation_Date],\r\n        [Procedure_Code],\r\n        [Patient_Name],\r\n        [Charge_Amount],\r\n        [Ins_Balance_Amount],\r\n        [Current_Insurance],\r\n        [Episode],\r\n        [Place],\r\n        [Account_Type],\r\n        [Description],\r\n        [Attorney],\r\n        [Practice],\r\n        [Resp_Level],\r\n        [Patient_Type],\r\n        [Resp_Payor],\r\n        [Assigned_To],\r\n        [Rendering_Provider],\r\n        [Referring_Provider],\r\n        [InsCd],\r\n        [Homeless_Status],\r\n        [Pri_Insurance_Name]", 1234, 1212);
                DisplayMethodHierarchy();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while loading Excel and inserting data.");
                throw; // Optionally rethrow the exception after logging
            }
        }
        private void DisplayMethodHierarchy()
        {
            // Ensure 'Program' class is in the correct namespace
            var methods = typeof(Program).GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(m => m.DeclaringType == typeof(Program))
                .ToList();

            Console.WriteLine("\nMethod Calling Hierarchy:");
            foreach (var method in methods)
            {
                Console.WriteLine($"- {method.Name}");
                // Here you could further explore what each method calls, 
                // but that requires a more in-depth analysis.
            }
        }


        private DataTable LoadExcelFileToDataTable(string excelFilePath)
        {
            // Load the Excel file into a DataTable
            using (var package = new ExcelPackage(new System.IO.FileInfo(excelFilePath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Get the first worksheet
                var dataTable = new DataTable();

                // Add columns to the DataTable
                foreach (var firstRowCell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
                {
                    dataTable.Columns.Add(firstRowCell.Text);
                }

                // Add rows to the DataTable
                for (var rowNumber = 2; rowNumber <= worksheet.Dimension.End.Row; rowNumber++)
                {
                    var row = new object[worksheet.Dimension.End.Column];
                    for (var col = 0; col < worksheet.Dimension.End.Column; col++)
                    {
                        row[col] = worksheet.Cells[rowNumber, col + 1].Text;
                    }
                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
        }

        //private readonly ILogger _logger;

        //public YourClassName(ILogger logger) // Inject the logger via constructor
        //{
        //    _logger = logger;
        //}

        private void TruncateTables()
        {
            Log.Information("Starting the truncation of tables.");

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        Log.Information("Truncating table Chlamg2.");
                        var command = new SqlCommand("TRUNCATE TABLE Chlamg2", connection, transaction);
                        command.ExecuteNonQuery();

                        Log.Information("Truncating table tbl_Inventory_Staging.");
                        var command1 = new SqlCommand("TRUNCATE TABLE tbl_Inventory_Staging", connection, transaction);
                        command1.ExecuteNonQuery();

                        transaction.Commit();
                        Log.Information("Successfully truncated tables.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error(ex, "An error occurred while truncating tables.");
                        throw; // or handle it as needed
                    }
                }
            }
        }


     
        private void BulkInsertDataTableToSqlServer(DataTable dataTable)
        {
            using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString)) // Use Microsoft.Data.SqlClient.SqlConnection
            {
                connection.Open();
                using (var bulkCopy = new Microsoft.Data.SqlClient.SqlBulkCopy(connection)) // Use Microsoft.Data.SqlClient.SqlBulkCopy
                {
                  //  bulkCopy.DestinationTableName = "dbo.tbl_TempPMCTexas_Invenotry"; // Ensure correct schema

                    //// Explicit column mappings
                    //bulkCopy.ColumnMappings.Add("Rendering Provider", "Rendering Provider");
                    //bulkCopy.ColumnMappings.Add("Payer Name", "Payer Name");
                    //bulkCopy.ColumnMappings.Add("Payer Address Line 1", "Payer Address Line 1");
                    //bulkCopy.ColumnMappings.Add("Payer Address Line 2", "Payer Address Line 2");
                    //bulkCopy.ColumnMappings.Add("Payer City", "Payer City");
                    //bulkCopy.ColumnMappings.Add("Payer State", "Payer State");
                    //bulkCopy.ColumnMappings.Add("Payer ZIP Code", "Payer ZIP Code");
                    //bulkCopy.ColumnMappings.Add("Payer Phone No", "Payer Phone No");
                    //bulkCopy.ColumnMappings.Add("Patient Name", "Patient Name");
                    //bulkCopy.ColumnMappings.Add("Patient Acct No", "Patient Acct No");
                    //bulkCopy.ColumnMappings.Add("Patient DOB", "Patient DOB");
                    //bulkCopy.ColumnMappings.Add("Payer Group No", "Payer Group No");
                    //bulkCopy.ColumnMappings.Add("Payer Subscriber No", "Payer Subscriber No");
                    //bulkCopy.ColumnMappings.Add("Aging Days", "Aging Days");
                    //bulkCopy.ColumnMappings.Add("Claim Date", "Claim Date");
                    //bulkCopy.ColumnMappings.Add("Service Date", "Service Date");
                    //bulkCopy.ColumnMappings.Add("Latest Transfer Date", "Latest Transfer Date");
                    //bulkCopy.ColumnMappings.Add("Last Submission Date", "Last Submission Date");
                    //bulkCopy.ColumnMappings.Add("Last Claim Status Change Date", "Last Claim Status Change Date");
                    //bulkCopy.ColumnMappings.Add("Claim No", "Claim No");
                    //bulkCopy.ColumnMappings.Add("Charges", "Charges");
                    //bulkCopy.ColumnMappings.Add("Claims Not Submitted", "Claims Not Submitted");
                    //bulkCopy.ColumnMappings.Add("Current", "Current");
                    //bulkCopy.ColumnMappings.Add("31-60", "31-60");
                    //bulkCopy.ColumnMappings.Add("61-90", "61-90");
                    //bulkCopy.ColumnMappings.Add("91-120", "91-120");
                    //bulkCopy.ColumnMappings.Add("> 120", "> 120");
                    //bulkCopy.ColumnMappings.Add("Balance", "Balance");
                    bulkCopy.DestinationTableName = "Chlamg2"; // Ensure correct schema
                    bulkCopy.ColumnMappings.Add("Claim ID", "Claim ID");
                    bulkCopy.ColumnMappings.Add("Username", "Username");
                    bulkCopy.ColumnMappings.Add("Worklist Status", "Worklist Status");
                    bulkCopy.ColumnMappings.Add("Pend Effective", "Pend Effective");
                    bulkCopy.ColumnMappings.Add("Pend Expires", "Pend Expires");
                    bulkCopy.ColumnMappings.Add("Payer", "Payer");
                    bulkCopy.ColumnMappings.Add("Custom Insurance Group", "Custom Insurance Group");
                    bulkCopy.ColumnMappings.Add("Insurance Package", "Insurance Package");
                    bulkCopy.ColumnMappings.Add("Outstanding Amount", "Outstanding Amount");
                    bulkCopy.ColumnMappings.Add("Date of Service", "Date of Service");
                    bulkCopy.ColumnMappings.Add("Diagnosis Codes", "Diagnosis Codes");
                    bulkCopy.ColumnMappings.Add("Procedure Code", "Procedure Code");
                    bulkCopy.ColumnMappings.Add("Hold Reason", "Hold Reason");
                    bulkCopy.ColumnMappings.Add("Hold Date", "Hold Date");
                    bulkCopy.ColumnMappings.Add("Days in Status", "Days in Status");
                    bulkCopy.ColumnMappings.Add("Primary Department", "Primary Department");
                    bulkCopy.ColumnMappings.Add("Patient Department", "Patient Department");
                    bulkCopy.ColumnMappings.Add("Service Department", "Service Department");
                    bulkCopy.ColumnMappings.Add("Supervising Provider", "Supervising Provider");
                    bulkCopy.ColumnMappings.Add("Rendering Provider", "Rendering Provider");
                    bulkCopy.ColumnMappings.Add("Referring Provider", "Referring Provider");
                    bulkCopy.ColumnMappings.Add("Patient Name", "Patient Name");
                    bulkCopy.ColumnMappings.Add("Worklist", "Worklist");
                    bulkCopy.ColumnMappings.Add("Last Claim Note", "Last Claim Note");
                    bulkCopy.ColumnMappings.Add("Claim Status", "Claim Status");
                    bulkCopy.ColumnMappings.Add("Specialty", "Specialty");
                    bulkCopy.ColumnMappings.Add("Escalated On", "Escalated On");




                    // Write the data to SQL Server
                    bulkCopy.WriteToServer(dataTable);
                }
            }
        }



        static void InputFieldsSelectedFields(String connectionString)
        {
            string query = @"
    INSERT INTO tbl_Inventory_Staging
    (
        [UID_Account_Number],
        [Claim_Number],
        [Exclusions],
        [Financial_Class],
        [Date_of_Service],
        [File_Creation_Date],
        [Procedure_Code],
        [Patient_Name],
        [Charge_Amount],
        [Ins_Balance_Amount],
        [Current_Insurance],
        [Episode],
        [Place],
        [Account_Type],
        [Description],
        [Attorney],
        [Practice],
        [Resp_Level],
        [Patient_Type],
        [Resp_Payor],
        [Assigned_To],
        [Rendering_Provider],
        [Referring_Provider],
        [InsCd],
        [Homeless_Status],
        [Pri_Insurance_Name]
    )
    SELECT
        [Claim ID],
        [Claim ID],
        [Payer],
        [Insurance Package],
        [Date of Service],
        NULL,  -- File_Creation_Date
        [Procedure Code],
        [Patient Name],
        [Outstanding Amount],
        [Outstanding Amount],  -- Ins_Balance_Amount
        NULL,  -- Current_Insurance
        [Custom Insurance Group],
        [Worklist Status],
        [Username],
        [Hold Reason],
        [Hold Date],
        [Specialty],
        [Diagnosis Codes],
        [Patient Department],
        [Service Department],
        [Supervising Provider],
        [Rendering Provider],
        [Referring Provider],
        [Last Claim Note],
        [Claim Status],
        [Payer]
    FROM Chlamg2;";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Log.Information("Database connection opened successfully.");

                    // Check if Chlamg2 has any data
                    string countQuery = "SELECT COUNT(*) FROM Chlamg2;";
                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        int totalRecords = (int)countCommand.ExecuteScalar();
                        Log.Information($"Total records in Chlamg2: {totalRecords}");

                        if (totalRecords == 0)
                        {
                            Log.Warning("No records found in Chlamg2. Skipping insertion into tbl_Inventory_Staging.");
                            return;
                        }
                    }

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        Log.Information("Executing query to insert data into tbl_Inventory_Staging...");

                        int rowsAffected = command.ExecuteNonQuery();
                        Log.Information($"{rowsAffected} rows inserted into tbl_Inventory_Staging.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while inserting data into tbl_Inventory_Staging.");
            }
            finally
            {
                Log.Information("Database operation completed.");
            }
        }
        public void UpdateInventoryStagingTable()
        {
            string projectName = "Chlamg Project"; // Example project name
            string projectID = "0924";             // Example project ID

            // Construct the SQL statement using the variables
            string sqlStmt = $@"
        UPDATE tbl_Inventory_Staging
        SET
            Claim_Number = (CASE WHEN (Claim_Number IS NULL AND UID_Account_Number IS NOT NULL) THEN UID_Account_Number ELSE Claim_Number END),
            UID_Account_Number = (CASE WHEN UID_Account_Number IS NULL AND Claim_Number IS NOT NULL THEN Claim_Number ELSE UID_Account_Number END),
            Process_Name = '{projectName}',
            Process_Id = '{projectID}',
            File_Creation_Date = (CASE WHEN File_Creation_Date IS NULL THEN GETDATE() ELSE File_Creation_Date END),
            IsActive = 1,
            User_Id = 0;";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    Log.Information("Database connection opened successfully for updating tbl_Inventory_Staging.");

                    using (SqlCommand command = new SqlCommand(sqlStmt, connection))
                    {
                        Log.Information("Executing update query on tbl_Inventory_Staging...");

                        int rowsAffected = command.ExecuteNonQuery();
                        Log.Information($"{rowsAffected} rows updated in tbl_Inventory_Staging.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while updating tbl_Inventory_Staging.");
            }
            finally
            {
                Log.Information("Update operation completed.");
            }
        }

        static void DeleteNullClaimNumbersFromInventoryStaging(string connectionString)
        {
            string deleteQuery = @"
        DELETE FROM tbl_Inventory_Staging
        WHERE Claim_Number IS NULL;";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Log.Information("Database connection opened successfully for deletion.");

                    using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                    {
                        Log.Information("Executing delete query on tbl_Inventory_Staging...");

                        int rowsAffected = command.ExecuteNonQuery();
                        Log.Information($"{rowsAffected} rows deleted from tbl_Inventory_Staging where Claim_Number is NULL.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while deleting rows from tbl_Inventory_Staging.");
            }
            finally
            {
                Log.Information("Delete operation completed.");
            }
        }

        static void HandleDuplicatesInInventoryStaging(string connectionString, int fileID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Log.Information("Database connection opened successfully.");

                    // Step 2: Handle duplicates and delete records where Date_of_Service is NULL
                    string deleteDuplicatesQuery = $@"
                ;WITH CTE_Duplicates AS
                (
                    SELECT A.*,
                           ROW_NUMBER() OVER(PARTITION BY Claim_Number
                                             ORDER BY CASE
                                                         WHEN [Current] IS NULL THEN Ins_Balance_Amount
                                                         ELSE [Current]
                                                      END ASC) AS rownumber,
                           {fileID} AS FileID,
                           'Service Date is NULL' AS Remarks
                    FROM tbl_Inventory_Staging A
                    WHERE Date_of_Service IS NULL
                )
                DELETE FROM CTE_Duplicates
                OUTPUT DELETED.* INTO tbl_Invenotry_Staging_Duplicates
                WHERE rownumber != 1;
            ";

                    using (SqlCommand deleteCommand = new SqlCommand(deleteDuplicatesQuery, connection))
                    {
                        Log.Information("Executing query to delete duplicate records with Date_of_Service IS NULL and insert into tbl_Invenotry_Staging_Duplicates...");

                        int rowsDeleted = deleteCommand.ExecuteNonQuery();
                        Log.Information($"{rowsDeleted} duplicate records deleted and inserted into tbl_Invenotry_Staging_Duplicates.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while handling duplicates in tbl_Inventory_Staging.");
            }
            finally
            {
                Log.Information("Operation for handling duplicates completed.");
            }
        }
        static void RemoveDuplicatesAndInsertIntoDuplicatesTable(string connectionString, int fileID)
        {
            // Define the command timeout duration (in seconds)
            int commandTimeout = 180;

            string query = @"
    ;WITH CTE_Duplicates_InFile AS
    (
        SELECT
            A.*,
            ROW_NUMBER() OVER(PARTITION BY Claim_Number ORDER BY A.Ins_Balance_Amount ASC) AS rownumber,
            @FileID AS FileID,
            'Duplicate Records in File' AS Remarks
        FROM tbl_Inventory_Staging A
    )
    DELETE FROM CTE_Duplicates_InFile
    OUTPUT DELETED.* INTO tbl_Invenotry_Staging_Duplicates
    WHERE rownumber != 1;

    DELETE FROM tbl_Invenotry_Staging_Duplicates WHERE Claim_Number IS NULL;";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Log.Information("Database connection opened successfully.");

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Set the command timeout
                        command.CommandTimeout = commandTimeout;

                        // Adding the FileID as a parameter to the query
                        command.Parameters.AddWithValue("@FileID", fileID);

                        Log.Information("Executing query to remove duplicates and insert into duplicates table...");

                        //int rowsAffected = command.ExecuteNonQuery();
                        //Log.Information($"{rowsAffected} rows affected during the deletion and insertion into the duplicates table.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while removing duplicates and inserting into the duplicates table.");
            }
            finally
            {
                Log.Information("Database operation completed.");
            }
        }



        static int GetDuplicateRecordCount(string connectionString, int fileID)
        {
            int count = 0;
            string query = @"
        SELECT @Cnt_DuplicateRecords = COUNT(*)
        FROM tbl_Invenotry_Staging_Duplicates
        WHERE FileID = @FileID AND Remarks = 'Duplicate Records in File';";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Log.Information("Database connection opened successfully.");

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Define the parameter
                        command.Parameters.AddWithValue("@FileID", fileID);

                        // Use output parameter to get the count
                        SqlParameter outputParameter = new SqlParameter("@Cnt_DuplicateRecords", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(outputParameter);

                        // Execute the command
                        command.ExecuteNonQuery();

                        // Retrieve the count from the output parameter
                        count = (int)(outputParameter.Value ?? 0); // Safely handle null
                        Log.Information($"Count of duplicate records: {count}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while retrieving the count of duplicate records.");
            }
            finally
            {
                Log.Information("Database operation completed.");
            }

            return count;
        }
        //    static void SequentialInsertProcess(string connectionString, string inventoryTable, string inputFields, int fileId, int projectId)
        //    {
        //        // Step 1: Create the ##temp table with differences from InventoryTable
        //        string createTempTableQuery = $@"
        //IF(OBJECT_ID('TEMPDB..##temp') IS NOT NULL)
        //    DROP TABLE ##temp;
        //SELECT * INTO ##temp FROM (
        //    SELECT
        //        a.UID_Account_Number AS UID_Account_Num,
        //        Practice AS Pract,
        //        Charge_Amount AS Charge_AMT,
        //        Ins_Balance_Amount AS Ins_Balance_Amt,
        //        Pri_Insurance_Name AS Pri_Insurance_Nme
        //    FROM
        //        tbl_Inventory_Staging a
        //    WHERE
        //        NOT EXISTS(
        //            SELECT 1
        //            FROM {inventoryTable} (NOLOCK) b
        //            WHERE a.UID_Account_Number COLLATE DATABASE_DEFAULT = b.UID_Account_Number COLLATE DATABASE_DEFAULT
        //        )
        //) AS TempResult;";

        //        // Step 2: Insert data from the staging table into InventoryTable using the ##temp table
        //        string insertDataQuery = $@"
        //INSERT INTO {inventoryTable}
        //SELECT
        //    {inputFields},
        //    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
        //    '{fileId}',
        //    0,
        //    '{projectId}',
        //    1,
        //    'DB',
        //    GETDATE(),
        //    'DB',
        //    GETDATE()
        //FROM
        //    tbl_Inventory_Staging Stg
        //JOIN
        //    ##temp T
        //ON
        //    Stg.UID_Account_Number = T.UID_Account_Num;";

        //        // Step 3: Get the total records loaded count
        //        string getTotalRecordsLoadedQuery = @"
        //SELECT COUNT(Stg.Claim_Number)
        //FROM tbl_Inventory_Staging Stg
        //JOIN ##temp T ON Stg.Claim_Number COLLATE DATABASE_DEFAULT = T.UID_Account_Num COLLATE DATABASE_DEFAULT;";

        //        // Step 4: Update balances in the InventoryTable
        //        string updateBalancesQuery = $@"
        //UPDATE CIP
        //SET
        //    CIP.Calculated_Current_Balance = Stg.Ins_Balance_Amount,
        //    CIP.Current_Balance = Stg.Ins_Balance_Amount,
        //    CIP.Payer_name = Stg.Pri_Insurance_Name
        //FROM {inventoryTable} CIP
        //JOIN tbl_Inventory_Staging Stg
        //ON CIP.UID_Account_Number COLLATE DATABASE_DEFAULT = Stg.UID_Account_Number COLLATE DATABASE_DEFAULT;";

        //        // Step 5: Delete entries from tbl_Inventory_Staging based on temp table
        //        string deleteFromStagingQuery = @"
        //DELETE Stg
        //FROM tbl_Inventory_Staging Stg
        //JOIN ##temp T ON Stg.Claim_Number COLLATE DATABASE_DEFAULT = T.UID_Account_Num COLLATE DATABASE_DEFAULT;";

        //        // Step 6: Get the count of existing records in tbl_Inventory_Staging
        //        string getExistingRecordsCountQuery = "SELECT COUNT(*) FROM tbl_Inventory_Staging;";

        //        // Step 7: Read data from ##temp to view what's inside
        //        string readTempTableQuery = "SELECT * FROM ##temp;";

        //        // Step 8: Insert existing records into tbl_Invenotry_Staging_Duplicates
        //        string insertDuplicatesQuery = $@"
        //INSERT INTO tbl_Invenotry_Staging_Duplicates
        //SELECT A.*, NULL, '{fileId}', N'Existing Records in Inventory' as Remarks
        //FROM tbl_Inventory_Staging A;";

        //        try
        //        {
        //            using (SqlConnection connection = new SqlConnection(connectionString))
        //            {
        //                connection.Open();

        //                // Step 1: Execute the temp table creation
        //                using (SqlCommand createTempCmd = new SqlCommand(createTempTableQuery, connection))
        //                {
        //                    createTempCmd.ExecuteNonQuery();
        //                    Console.WriteLine("Temporary table created successfully.");
        //                }

        //                // Step 2: Insert data into inventory table
        //                using (SqlCommand insertDataCmd = new SqlCommand(insertDataQuery, connection))
        //                {
        //                    insertDataCmd.ExecuteNonQuery();
        //                    Console.WriteLine("Data inserted into inventory table successfully.");
        //                }

        //                // Step 3: Get the total records loaded count
        //                int totalRecordsLoaded = 0;
        //                using (SqlCommand totalRecordsCmd = new SqlCommand(getTotalRecordsLoadedQuery, connection))
        //                {
        //                    totalRecordsLoaded = (int)totalRecordsCmd.ExecuteScalar(); // Get the count result
        //                    Console.WriteLine($"Total Records Loaded from Staging Table: {totalRecordsLoaded}");
        //                }

        //                // Step 4: Update balances in the InventoryTable
        //                using (SqlCommand updateBalancesCmd = new SqlCommand(updateBalancesQuery, connection))
        //                {
        //                    int rowsAffected = updateBalancesCmd.ExecuteNonQuery(); // Execute the update query
        //                    Console.WriteLine($"{rowsAffected} records updated in the inventory table successfully.");
        //                }

        //                // Step 5: Delete entries from tbl_Inventory_Staging
        //                int deletedRowsCount = 0;
        //                using (SqlCommand deleteFromStagingCmd = new SqlCommand(deleteFromStagingQuery, connection))
        //                {
        //                    deletedRowsCount = deleteFromStagingCmd.ExecuteNonQuery(); // Execute the delete query
        //                    Console.WriteLine($"{deletedRowsCount} records deleted from the staging table successfully.");
        //                }

        //                // Step 6: Get the count of existing records in tbl_Inventory_Staging
        //                int existingRecordsCount = 0;
        //                using (SqlCommand existingRecordsCmd = new SqlCommand(getExistingRecordsCountQuery, connection))
        //                {
        //                    existingRecordsCount = (int)existingRecordsCmd.ExecuteScalar(); // Get the count of existing records
        //                    Console.WriteLine($"Existing Records ExistingRecords: {existingRecordsCount}");
        //                }

        //                // Step 7: Read and display data from ##temp table
        //                using (SqlCommand readTempCmd = new SqlCommand(readTempTableQuery, connection))
        //                {
        //                    using (SqlDataReader reader = readTempCmd.ExecuteReader())
        //                    {
        //                        if (reader.HasRows)
        //                        {
        //                            while (reader.Read())
        //                            {
        //                                // Display each row from the ##temp table
        //                                Console.WriteLine($"UID_Account_Num: {reader["UID_Account_Num"]}, Practice: {reader["Pract"]}, Charge_AMT: {reader["Charge_AMT"]}, Ins_Balance_Amt: {reader["Ins_Balance_Amt"]}, Pri_Insurance_Nme: {reader["Pri_Insurance_Nme"]}");
        //                            }
        //                        }
        //                        else
        //                        {
        //                            Console.WriteLine("No data found in ##temp table.");
        //                        }
        //                    }
        //                }

        //                // Step 8: Insert existing records into tbl_Invenotry_Staging_Duplicates
        //                using (SqlCommand insertDuplicatesCmd = new SqlCommand(insertDuplicatesQuery, connection))
        //                {
        //                    int duplicatesInserted = insertDuplicatesCmd.ExecuteNonQuery(); // Execute the insert duplicates query
        //                    Console.WriteLine($"{duplicatesInserted} duplicate records inserted into tbl_Invenotry_Staging_Duplicates.");
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            // Handle or log the exception
        //            Console.WriteLine($"Error: {ex.Message}");
        //        }
        //    }
        static void SequentialInsertProcess(string connectionString, string inventoryTable, string inputFields, int fileId, int projectId)
        {
            // Step 1: Create the ##temp table with differences from InventoryTable
            string createTempTableQuery = $@"
    IF(OBJECT_ID('TEMPDB..##temp') IS NOT NULL)
        DROP TABLE ##temp;
    SELECT * INTO ##temp FROM (
        SELECT
            a.UID_Account_Number AS UID_Account_Num,
            Practice AS Pract,
            Charge_Amount AS Charge_AMT,
            Ins_Balance_Amount AS Ins_Balance_Amt,
            Pri_Insurance_Name AS Pri_Insurance_Nme
        FROM
            tbl_Inventory_Staging a
        WHERE
            NOT EXISTS(
                SELECT 1
                FROM {inventoryTable} (NOLOCK) b
                WHERE a.UID_Account_Number COLLATE DATABASE_DEFAULT = b.UID_Account_Number COLLATE DATABASE_DEFAULT
            )
    ) AS TempResult;";

            // Step 2: Insert data from the staging table into InventoryTable using the ##temp table
            string insertDataQuery = $@"
    INSERT INTO {inventoryTable}
    SELECT
        {inputFields},
        NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
        '{fileId}',
        0,
        '{projectId}',
        1,
        'DB',
        GETDATE(),
        'DB',
        GETDATE()
    FROM
        tbl_Inventory_Staging Stg
    JOIN
        ##temp T
    ON
        Stg.UID_Account_Number = T.UID_Account_Num;";

            // Step 3: Get the total records loaded count
            string getTotalRecordsLoadedQuery = @"
    SELECT COUNT(Stg.Claim_Number)
    FROM tbl_Inventory_Staging Stg
    JOIN ##temp T ON Stg.Claim_Number COLLATE DATABASE_DEFAULT = T.UID_Account_Num COLLATE DATABASE_DEFAULT;";

            // Step 4: Update balances in the InventoryTable
            string updateBalancesQuery = $@"
    UPDATE CIP
    SET
        CIP.Calculated_Current_Balance = Stg.Ins_Balance_Amount,
        CIP.Current_Balance = Stg.Ins_Balance_Amount,
        CIP.Payer_name = Stg.Pri_Insurance_Name
    FROM {inventoryTable} CIP
    JOIN tbl_Inventory_Staging Stg
    ON CIP.UID_Account_Number COLLATE DATABASE_DEFAULT = Stg.UID_Account_Number COLLATE DATABASE_DEFAULT;";

            // Step 5: Delete entries from tbl_Inventory_Staging based on temp table
            string deleteFromStagingQuery = @"
    DELETE Stg
    FROM tbl_Inventory_Staging Stg
    JOIN ##temp T ON Stg.Claim_Number COLLATE DATABASE_DEFAULT = T.UID_Account_Num COLLATE DATABASE_DEFAULT;";

            // Step 6: Get the count of existing records in tbl_Inventory_Staging
            string getExistingRecordsCountQuery = "SELECT COUNT(*) FROM tbl_Inventory_Staging;";

            // Step 7: Read data from ##temp to view what's inside
            string readTempTableQuery = "SELECT * FROM ##temp;";

            // Step 8: Insert existing records into tbl_Invenotry_Staging_Duplicates
            string insertDuplicatesQuery = $@"
    INSERT INTO tbl_Invenotry_Staging_Duplicates
    SELECT A.*, NULL, '{fileId}', N'Existing Records in Inventory' as Remarks
    FROM tbl_Inventory_Staging A;";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Step 1: Execute the temp table creation
                    using (SqlCommand createTempCmd = new SqlCommand(createTempTableQuery, connection))
                    {
                        createTempCmd.ExecuteNonQuery();
                        Console.WriteLine("Temporary table created successfully.");
                    }

                    // Step 2: Insert data into inventory table
                    using (SqlCommand insertDataCmd = new SqlCommand(insertDataQuery, connection))
                    {
                        insertDataCmd.ExecuteNonQuery();
                        Console.WriteLine("Data inserted into inventory table successfully.");
                    }

                    // Step 3: Get the total records loaded count
                    int totalRecordsLoaded = 0;
                    using (SqlCommand totalRecordsCmd = new SqlCommand(getTotalRecordsLoadedQuery, connection))
                    {
                        totalRecordsLoaded = (int)totalRecordsCmd.ExecuteScalar(); // Get the count result
                        Console.WriteLine($"Total Records Loaded from Staging Table: {totalRecordsLoaded}");
                    }

                    // Step 4: Update balances in the InventoryTable
                    using (SqlCommand updateBalancesCmd = new SqlCommand(updateBalancesQuery, connection))
                    {
                        int rowsAffected = updateBalancesCmd.ExecuteNonQuery(); // Execute the update query
                        Console.WriteLine($"{rowsAffected} records updated in the inventory table successfully.");
                    }

                    // Step 5: Delete entries from tbl_Inventory_Staging
                    int deletedRowsCount = 0;
                    using (SqlCommand deleteFromStagingCmd = new SqlCommand(deleteFromStagingQuery, connection))
                    {
                        deletedRowsCount = deleteFromStagingCmd.ExecuteNonQuery(); // Execute the delete query
                        Console.WriteLine($"{deletedRowsCount} records deleted from the staging table successfully.");
                    }

                    // Step 6: Get the count of existing records in tbl_Inventory_Staging
                    int existingRecordsCount = 0;
                    using (SqlCommand existingRecordsCmd = new SqlCommand(getExistingRecordsCountQuery, connection))
                    {
                        existingRecordsCount = (int)existingRecordsCmd.ExecuteScalar(); // Get the count of existing records
                        Console.WriteLine($"Existing Records in Staging Table: {existingRecordsCount}");
                    }

                    // Step 7: Read and display data from ##temp table
                    using (SqlCommand readTempCmd = new SqlCommand(readTempTableQuery, connection))
                    {
                        using (SqlDataReader reader = readTempCmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    // Display each row from the ##temp table
                                    Console.WriteLine($"UID_Account_Num: {reader["UID_Account_Num"]}, Practice: {reader["Pract"]}, Charge_AMT: {reader["Charge_AMT"]}, Ins_Balance_Amt: {reader["Ins_Balance_Amt"]}, Pri_Insurance_Nme: {reader["Pri_Insurance_Nme"]}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("No data found in ##temp table.");
                            }
                        }
                    }

                    // Step 8: Insert existing records into tbl_Invenotry_Staging_Duplicates
                    using (SqlCommand insertDuplicatesCmd = new SqlCommand(insertDuplicatesQuery, connection))
                    {
                        int duplicatesInserted = insertDuplicatesCmd.ExecuteNonQuery(); // Execute the insert duplicates query
                        Console.WriteLine($"{duplicatesInserted} duplicate records inserted into tbl_Invenotry_Staging_Duplicates.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}

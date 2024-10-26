//using ChlamgMVC.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Data.SqlClient;
//using Serilog;
using ChlamgMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Serilog; // Import Serilog for logging
using System;
using System.Collections.Generic;


namespace ChlamgMVC.Controllers
{
    public class DataController : Controller
    {
        private readonly string _connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=New Database;Integrated Security=True;";

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        //public IActionResult LoadData()
        //{
        //    try
        //    {
        //        // Log the start of the Excel load operation
        //        //string excelFilePath = @"C:\Users\srikanthpa.DATAMARSHALL\Downloads\allclaims1.xlsx"; // Update file path as needed
        //        string excelFilePath = @"C:\Users\srikanthpa.DATAMARSHALL\Downloads\14ProcessUploading\pmc_Texas31.09 - Payer Claim Aging - Detail (3).xlsx";
        //        Log.Information("Attempting to load Excel file from path: {FilePath}", excelFilePath);
        //        //"C:\Users\srikanthpa.DATAMARSHALL\Downloads\14ProcessUploading\pmc_Texas31.09 - Payer Claim Aging - Detail (3).xlsx"
        //        // Create an instance of the DataModel and load the Excel file into the database
        //        var dataModel = new DataModel(_connectionString);
        //        dataModel.LoadExcelAndInsertData(excelFilePath);

        //        // Log success and update the ViewBag with a success message
        //        Log.Information("Excel data loaded successfully from {FilePath}", excelFilePath);
        //        ViewBag.Message = "Data Loaded Successfully!";

        //        // Call the method to retrieve data from the database and pass it to the view
        //        return ViewDataFromTable();
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the error with exception details and display an error message in the view
        //        //Log.Error(ex, "Error occurred while loading Excel data from {FilePath}", excelFilePath);
        //        ViewBag.Message = $"Error: {ex.Message}";
        //    }

        //    // Return the Index view, showing either a success or error message
        //    return View("Index");
        //}
        

        // Other methods (ViewDataFromTable and ViewDataFromPmctexastemp) remain unchanged

        // Method to fetch data from Chlamg2 table and pass it to the view
        public IActionResult ViewDataFromTable()
        {
            List<Chlamg2Model> dataList = new List<Chlamg2Model>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Chlamg2"; // Query to fetch all data from Chlamg2
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Chlamg2Model data = new Chlamg2Model
                    {
                        ClaimID = (int)reader["Claim ID"],
                        Username = reader["Username"]?.ToString(),
                        WorklistStatus = reader["Worklist Status"]?.ToString(),
                        PendEffective = reader["Pend Effective"] as DateTime?,
                        PendExpires = reader["Pend Expires"] as DateTime?,
                        Payer = reader["Payer"]?.ToString(),
                        CustomInsuranceGroup = reader["Custom Insurance Group"]?.ToString(),
                        InsurancePackage = reader["Insurance Package"]?.ToString(),
                        OutstandingAmount = reader["Outstanding Amount"] as decimal?,
                        DateOfService = reader["Date of Service"] as DateTime?,
                        DiagnosisCodes = reader["Diagnosis Codes"]?.ToString(),
                        ProcedureCode = reader["Procedure Code"]?.ToString(),
                        HoldReason = reader["Hold Reason"]?.ToString(),
                        HoldDate = reader["Hold Date"] as DateTime?,
                        DaysInStatus = reader["Days in Status"] as int?,
                        PrimaryDepartment = reader["Primary Department"]?.ToString(),
                        PatientDepartment = reader["Patient Department"]?.ToString(),
                        ServiceDepartment = reader["Service Department"]?.ToString(),
                        SupervisingProvider = reader["Supervising Provider"]?.ToString(),
                        RenderingProvider = reader["Rendering Provider"]?.ToString(),
                        ReferringProvider = reader["Referring Provider"]?.ToString(),
                        PatientName = reader["Patient Name"]?.ToString(),
                        Worklist = reader["Worklist"]?.ToString(),
                        LastClaimNote = reader["Last Claim Note"]?.ToString(),
                        ClaimStatus = reader["Claim Status"]?.ToString(),
                        Specialty = reader["Specialty"]?.ToString(),
                        EscalatedOn = reader["Escalated On"] as DateTime?
                    };
                    dataList.Add(data); // Add each record to the dataList
                }
                conn.Close();
            }

            return View("ViewDataFromTable", dataList); // Pass the data list to the view
        }
        public async Task<IActionResult> ViewDataFromPmctexastempAsync(int pageNumber = 0, int pageSize = 500)
        {
            List<PmctexastempModel> dataList = new List<PmctexastempModel>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = $@"
            SELECT * FROM tbl_TempPMCTexas_Invenotry
            ORDER BY [YourSortingColumn]
            OFFSET {pageNumber * pageSize} ROWS
            FETCH NEXT {pageSize} ROWS ONLY;";

                SqlCommand cmd = new SqlCommand(query, conn);
                await conn.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    PmctexastempModel data = new PmctexastempModel
                    {
                        // Map the columns from your pmctexastemp table to the model properties
                        RenderingProvider = reader["Rendering Provider"]?.ToString(),
                        PayerName = reader["Payer Name"]?.ToString(),
                        PayerAddressLine1 = reader["Payer Address Line 1"]?.ToString(),
                        PayerAddressLine2 = reader["Payer Address Line 2"]?.ToString(),
                        PayerCity = reader["Payer City"]?.ToString(),
                        PayerState = reader["Payer State"]?.ToString(),
                        PayerZipCode = reader["Payer ZIP Code"]?.ToString(),
                        PayerPhoneNo = reader["Payer Phone No"]?.ToString(),
                        PatientName = reader["Patient Name"]?.ToString(),
                        PatientAcctNo = reader["Patient Acct No"]?.ToString(),
                        PatientDob = reader["Patient DOB"] as DateTime?,
                        PayerGroupNo = reader["Payer Group No"]?.ToString(),
                        PayerSubscriberNo = reader["Payer Subscriber No"]?.ToString(),
                        AgingDays = reader["Aging Days"] != DBNull.Value && !string.IsNullOrEmpty(reader["Aging Days"].ToString())
                    ? Convert.ToInt32(reader["Aging Days"])
                    : 0,
                        ClaimDate = reader["Claim Date"] as DateTime?,
                        ServiceDate = reader["Service Date"] as DateTime?,
                        LatestTransferDate = reader["Latest Transfer Date"] as DateTime?,
                        LastSubmissionDate = reader["Last Submission Date"] as DateTime?,
                        LastClaimStatusChangeDate = reader["Last Claim Status Change Date"] as DateTime?,
                        ClaimNo = reader["Claim No"]?.ToString(),
                        Charges = reader["Charges"] != DBNull.Value && !string.IsNullOrEmpty(reader["Charges"].ToString())
                    ? Convert.ToDecimal(reader["Charges"])
                    : 0,
                        ClaimsNotSubmitted = reader["Claims Not Submitted"] != DBNull.Value && !string.IsNullOrEmpty(reader["Claims Not Submitted"].ToString())
                    ? Convert.ToDecimal(reader["Claims Not Submitted"])
                    : 0,
                        Current = reader["Current"] != DBNull.Value && !string.IsNullOrEmpty(reader["Current"].ToString())
                    ? Convert.ToDecimal(reader["Current"])
                    : 0,
                        ThirtyOneToSixty = reader["31-60"] != DBNull.Value && !string.IsNullOrEmpty(reader["31-60"].ToString())
                    ? Convert.ToDecimal(reader["31-60"])
                    : 0,
                        SixtyOneToNinety = reader["61-90"] != DBNull.Value && !string.IsNullOrEmpty(reader["61-90"].ToString())
                    ? Convert.ToDecimal(reader["61-90"])
                    : 0,
                        NinetyOneToOneTwenty = reader["91-120"] != DBNull.Value && !string.IsNullOrEmpty(reader["91-120"].ToString())
                    ? Convert.ToDecimal(reader["91-120"])
                    : 0,
                        OverOneTwenty = reader["> 120"] != DBNull.Value && !string.IsNullOrEmpty(reader["> 120"].ToString())
                    ? Convert.ToDecimal(reader["> 120"])
                    : 0,
                        Balance = reader["Balance"] != DBNull.Value && !string.IsNullOrEmpty(reader["Balance"].ToString())
                    ? Convert.ToDecimal(reader["Balance"])
                    : 0
                    };
                    dataList.Add(data); // Add each record to the dataList
                }
               
                await reader.CloseAsync();
            }

            return View("ViewDataFromPmctexastemp", dataList);
        }

    }
}

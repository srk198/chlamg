namespace ChlamgMVC.Models
{
    public class Chlamg2Model
    {
        public int ClaimID { get; set; }
        public string Username { get; set; }
        public string WorklistStatus { get; set; }
        public DateTime? PendEffective { get; set; }
        public DateTime? PendExpires { get; set; }
        public string Payer { get; set; }
        public string CustomInsuranceGroup { get; set; }
        public string InsurancePackage { get; set; }
        public decimal? OutstandingAmount { get; set; }
        public DateTime? DateOfService { get; set; }
        public string DiagnosisCodes { get; set; }
        public string ProcedureCode { get; set; }
        public string HoldReason { get; set; }
        public DateTime? HoldDate { get; set; }
        public int? DaysInStatus { get; set; }
        public string PrimaryDepartment { get; set; }
        public string PatientDepartment { get; set; }
        public string ServiceDepartment { get; set; }
        public string SupervisingProvider { get; set; }
        public string RenderingProvider { get; set; }
        public string ReferringProvider { get; set; }
        public string PatientName { get; set; }
        public string Worklist { get; set; }
        public string LastClaimNote { get; set; }
        public string ClaimStatus { get; set; }
        public string Specialty { get; set; }
        public DateTime? EscalatedOn { get; set; }
    }
    public class PmctexastempModel
    {
        public int Id { get; set; } // Primary key (if exists)

        // Columns from the pmctexas file
        public string RenderingProvider { get; set; }
        public string PayerName { get; set; }
        public string PayerAddressLine1 { get; set; }
        public string PayerAddressLine2 { get; set; }
        public string PayerCity { get; set; }
        public string PayerState { get; set; }
        public string PayerZipCode { get; set; }
        public string PayerPhoneNo { get; set; }
        public string PatientName { get; set; }
        public string PatientAcctNo { get; set; }
        public DateTime? PatientDob { get; set; } // Nullable DateTime in case some records are missing DOB
        public string PayerGroupNo { get; set; }
        public string PayerSubscriberNo { get; set; }
        public int AgingDays { get; set; }
        public DateTime? ClaimDate { get; set; }
        public DateTime? ServiceDate { get; set; }
        public DateTime? LatestTransferDate { get; set; }
        public DateTime? ClaimFirstSubmittedDate { get; set; }
        public DateTime? LastSubmissionDate { get; set; }
        public DateTime? LastClaimStatusChangeDate { get; set; }
        public string ClaimNo { get; set; }
        public decimal Charges { get; set; }
        public decimal ClaimsNotSubmitted { get; set; }
        public decimal Current { get; set; }
        public decimal ThirtyOneToSixty { get; set; }
        public decimal SixtyOneToNinety { get; set; }
        public decimal NinetyOneToOneTwenty { get; set; }
        public decimal OverOneTwenty { get; set; }
        public decimal Balance { get; set; }

        // Additional properties (if needed)
        public DateTime? CreatedDate { get; set; } // Record creation date
        public string Status { get; set; } // Example: Claim status
        public string Description { get; set; } // Example: Additional description
    }
}

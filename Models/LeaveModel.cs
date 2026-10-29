namespace SmartOfficeSystem.Models
{
    public class LeaveModel
    {
        public int LeaveId { get; set; }
        public string? EmployeeName { get; set; }
        public string? LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
    }
}
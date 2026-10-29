namespace SmartOfficeSystem.Models
{
    public class AttendanceModel
    {
        public int AttendanceId { get; set; }
        public string? EmployeeName { get; set; }
        public string? Date { get; set; }
        public string? PunchIn { get; set; }
        public string? PunchOut { get; set; }
        public string? Status { get; set; }
    }
}
namespace SmartOfficeSystem.Models
{
    public class Employee
    {
        public int Id { get; set; } // [cite: 25]
        
        
        // ? lagane se error chala jayega, iska matlab hai "Optional"
        public string? Name { get; set; } = string.Empty; 
        
        public string? Email { get; set; } = string.Empty;
        
        public string? Department { get; set; } = string.Empty; // [cite: 19, 25]
        
        public double Salary { get; set; } // [cite: 22]
        public int Progress { get; set; }
    }
}


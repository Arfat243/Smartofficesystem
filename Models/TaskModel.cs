using System.ComponentModel.DataAnnotations;

namespace SmartOfficeSystem.Models
{
    public class TaskModel
    {
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Task Name zaruri hai!")]
        public string? TaskName { get; set; }

        [Required(ErrorMessage = "Description bharna zaruri hai!")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Employee select karo!")]
        public int AssignedTo { get; set; }

        public string? AssignedBy { get; set; }

        [Required(ErrorMessage = "Deadline batani padegi!")]
        public DateTime Deadline { get; set; } = DateTime.Now;

        public string? Status { get; set; } = "Pending";
    
    public string Priority { get; set; } = "Medium"; 
    public int Progress { get; set; }
        public object? Title { get; internal set; }
    }
public class SubTask {
    public int SubTaskId { get; set; }
    public int MainTaskId { get; set; }
    public required string Description { get; set; }
    public bool IsCompleted { get; set; }
}}

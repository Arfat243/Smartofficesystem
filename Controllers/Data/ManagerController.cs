using Microsoft.AspNetCore.Mvc;
using SmartOfficeSystem.Data;
using SmartOfficeSystem.Models;
using Microsoft.AspNetCore.Http;

namespace SmartOfficeSystem.Controllers
{
    public class ManagerController : Controller
    {
        private readonly ApplicationDbHandler _dbHandler;

        public ManagerController(ApplicationDbHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        // Security Check helper
        private bool IsManager()
        {
            return HttpContext.Session.GetString("UserRole") == "Manager";
        }

        public IActionResult Index()
        {
            if (!IsManager()) return RedirectToAction("Login", "Account");

            var allTasks = _dbHandler.GetAllTasksWithEmployeeNames();
           ViewBag.Notices = _dbHandler.GetAllNotices();
            return View(allTasks);
        }

        [HttpGet]
        public IActionResult AssignTask()
        {
            if (!IsManager()) return RedirectToAction("Login", "Account");

            ViewBag.Employees = _dbHandler.GetAllEmployees();
            return View();
        }

        [HttpPost]
[HttpPost]
public IActionResult AssignTask(TaskModel task, List<string> subTasks) // <-- Yahan List<string> add kiya
{
    if (!IsManager()) return RedirectToAction("Login", "Account");

    task.AssignedBy = HttpContext.Session.GetString("UserSession"); 
    
    // 1. Pehle Main Task save karenge aur uski ID le aayenge
    // Note: Iske liye ApplicationDbHandler mein return type 'int' hona chahiye (ID ke liye)
    int newTaskId = _dbHandler.AssignTaskWithId(task); 

    if (newTaskId > 0)
    {
        // 2. Ab Sub-Tasks save karenge jo list mein aaye hain
        if (subTasks != null && subTasks.Count > 0)
        {
            _dbHandler.SaveSubTasks(newTaskId, subTasks);
        }

        TempData["success"] = "Main Task & Sub-Tasks Assigned! 🚀";
        return RedirectToAction("Index");
    }
    
    ViewBag.Employees = _dbHandler.GetAllEmployees();
    return View(task);
}
        [HttpPost]
        public IActionResult DeleteTask(int taskId)
        {
            if (!IsManager()) return RedirectToAction("Login", "Account");

            if (_dbHandler.DeleteTask(taskId))
            {
                TempData["success"] = "Task Permanently Deleted! 🗑️";
            }
            return RedirectToAction("Index");
        }

        // --- LEAVE MANAGEMENT SECTION ---

        public IActionResult ReviewLeaves()
        {
            if (!IsManager()) return RedirectToAction("Login", "Account");

            // Database se saari leaves le kar aayega
            var allLeaves = _dbHandler.GetAllLeaves();
            return View(allLeaves);
        }

        [HttpPost]
        public IActionResult UpdateLeaveStatus(int leaveId, string status)
        {
            if (!IsManager()) return RedirectToAction("Login", "Account");

            // ApplicationDbHandler ke 'public bool UpdateLeaveStatus' ko call karega
            bool result = _dbHandler.UpdateLeaveStatus(leaveId, status);
            
            if(result)
            {
                TempData["success"] = $"Leave {status} successfully! ✅";
            }
            else
            {
                TempData["error"] = "Status update karne mein dikat aayi.";
            }

            return RedirectToAction("ReviewLeaves");
        }
        [HttpPost]
public IActionResult DeleteNotice(int id)
{
    bool isDeleted = _dbHandler.DeleteNotice(id);
    
    if (isDeleted)
    {
        TempData["Success"] = "Notice successfully delete ho gaya!";
    }
    else
    {
        TempData["Error"] = "Notice delete nahi ho paya, check karein.";
    }

    // Wapas usi page par bhej do jahan se aaya tha
    return RedirectToAction("NoticeBoard"); 
}
// 1. Ye method AddNotice page ko dikhayega
[HttpGet]
public IActionResult AddNotice()
{
    return View();
}

// 2. Ye method form ka data database mein save karega
[HttpPost]
public IActionResult AddNotice(Data.NoticeModel model)
{
    if (ModelState.IsValid)
    {
        // Session se manager ka naam uthao
        var userSession = HttpContext.Session.GetString("UserSession");
        model.PostedBy = userSession ?? "Manager"; 
        model.PostedDate = DateTime.Now;

        _dbHandler.AddNotice(model);
        return RedirectToAction("Index");
    }
    return View(model);
}    }
}
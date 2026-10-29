using Microsoft.AspNetCore.Mvc;
using SmartOfficeSystem.Data;
using SmartOfficeSystem.Models;

namespace SmartOfficeSystem.Controllers
{
    public class EmployeePortalController : Controller
    {
        private readonly ApplicationDbHandler _db;

        public EmployeePortalController(ApplicationDbHandler db)
        {
            _db = db;
        }

        public IActionResult EmployeeDashboard()
        {
            var username = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");
             ViewBag.AllNotices = _db.GetActiveNotices();
            var tasks = _db.GetTasksForEmployee(username);
            ViewBag.LeaveHistory = _db.GetLeavesForEmployee(username);

            // Ye check karega ki button "Punch In" dikhana hai ya "Punch Out"
            ViewBag.IsPunchedIn = _db.IsCurrentlyPunchedIn(username);
            
            // Ye teri purani attendance list (aaj ki)
            ViewBag.TodayAttendanceList = _db.GetTodayAttendance(username); 

            // --- NEW METHOD ADDED (DELETE NAHI KIYA KUCH BHI) ---
            // Isse hum saari purani attendance history nikaalenge dashboard ke liye
            ViewBag.AttendanceHistory = _db.GetAttendanceHistory(username); 

            return View(tasks);
        }
        [HttpPost]
public IActionResult UpdateProgress(int taskId, int progress)
{
    // Tera banaya hua ADO.NET method call ho raha hai
    _db.UpdateTaskProgress(taskId, progress);
    
    // Kaam hone ke baad wapas dashboard par bhej do
    return RedirectToAction("EmployeeDashboard");
}
        

        [HttpPost]
        public IActionResult MarkComplete(int taskId)
        {
            _db.CompleteTask(taskId);
            return RedirectToAction("EmployeeDashboard"); 
        }

        [HttpPost]
        public IActionResult PunchIn()
        {
            var username = HttpContext.Session.GetString("UserSession");
            if (!string.IsNullOrEmpty(username))
            {
                _db.PunchInEnhanced(username); 
            }
            return RedirectToAction("EmployeeDashboard"); 
        }

        [HttpPost]
        public IActionResult PunchOut()
        {
            var username = HttpContext.Session.GetString("UserSession");
            if (!string.IsNullOrEmpty(username))
            {
                _db.PunchOutEnhanced(username); 
            }
            return RedirectToAction("EmployeeDashboard"); 
        }

        [HttpGet]
        public IActionResult ApplyLeave()
        {
            return View();
        }

[HttpPost]
[ValidateAntiForgeryToken] 
public IActionResult ApplyLeave(SmartOfficeSystem.Models.LeaveModel model)
{
    // 1. Session se naam nikaalo taaki DB mein pata chale kiski leave hai
    var username = HttpContext.Session.GetString("UserSession");
    
    if (string.IsNullOrEmpty(username)) 
    {
        return RedirectToAction("Login", "Account");
    }

    // 2. Model mein Employee ka naam bharo
    model.EmployeeName = username;

    if (ModelState.IsValid)
    {
        // 3. ASLI KAAM: Database mein save karne ke liye call karo
        // Pehle tum sirf redirect kar rahe the, ab save kar rahe ho
        bool isSaved = _db.ApplyLeave(model); 
        
        if (isSaved)
        {
            return RedirectToAction("EmployeeDashboard");
        }
    }
    return View(model);
}    }
}
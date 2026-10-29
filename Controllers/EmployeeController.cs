using Microsoft.AspNetCore.Mvc;
using SmartOfficeSystem.Data;
using SmartOfficeSystem.Models;
using Microsoft.AspNetCore.Http;
using ClosedXML.Excel;
using System.IO;
using System.Linq;

namespace SmartOfficeSystem.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbHandler _dbHandler;
        private string? employees;

        public EmployeeController(ApplicationDbHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        private bool IsNotLoggedIn()
        {
            return string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));
        }

        // --- EMPLOYEE DASHBOARD ---
        public IActionResult EmployeeDashboard()
        {
            if (IsNotLoggedIn()) return RedirectToAction("Login", "Account");

            string? username = HttpContext.Session.GetString("UserSession");

            // 1. Tasks Lana
            var myTasks = _dbHandler.GetTasksForEmployee(username) ?? new List<TaskModel>();
            myTasks.Reverse();

            // 2. Attendance Data (Yeh line missing thi!)
            ViewBag.TodayAttendance = _dbHandler.GetTodayAttendance(username);

            // 3. Leave History Data (Yeh line bhi missing thi!)
            ViewBag.LeaveHistory = _dbHandler.GetLeavesForEmployee(username);

            // 4. Stats
            ViewBag.TotalTasks = myTasks.Count;
            ViewBag.PendingTasks = myTasks.Count(t => t.Status?.ToLower() == "pending");
            ViewBag.CompletedTasks = myTasks.Count(t => t.Status?.ToLower() == "completed");

            return View(myTasks);
        }

        // --- ATTENDANCE METHODS (Jo tumne add nahi kiye the) ---
        [HttpPost]
        public IActionResult PunchIn()
        {
            if (IsNotLoggedIn()) return RedirectToAction("Login", "Account");
            string? username = HttpContext.Session.GetString("UserSession");
            _dbHandler.PunchIn(username);
            return RedirectToAction("EmployeeDashboard");
        }

        [HttpPost]
        public IActionResult PunchOut()
        {
            if (IsNotLoggedIn()) return RedirectToAction("Login", "Account");
            string? username = HttpContext.Session.GetString("UserSession");
            _dbHandler.PunchOut(username);
            return RedirectToAction("EmployeeDashboard");
        }

        [HttpPost]
        public IActionResult MarkComplete(int taskId)
        {
            if (IsNotLoggedIn()) return RedirectToAction("Login", "Account");
            _dbHandler.CompleteTask(taskId);
            return RedirectToAction("EmployeeDashboard");
        }

        // --- LEAVE METHODS ---
        public IActionResult ApplyLeave()
        {
            if (IsNotLoggedIn()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public IActionResult ApplyLeave(LeaveModel lv)
        {
            if (IsNotLoggedIn()) return RedirectToAction("Login", "Account");
            lv.EmployeeName = HttpContext.Session.GetString("UserSession");
            
            if(_dbHandler.ApplyLeave(lv)) {
                TempData["success"] = "Chutti ki request bhej di gayi hai! ✈️";
                return RedirectToAction("EmployeeDashboard");
            }
            return View(lv);
        }

        // --- BAKI PURANE METHODS (Index, Create, Delete, Excel) ---
public IActionResult Index(string searchString)
{
    if (IsNotLoggedIn()) return RedirectToAction("Login", "Account");

    // --- YE LINE ADD KARNI HAI (Notice Board ka data laane ke liye) ---
    ViewBag.AllNotices = _dbHandler.GetActiveNotices(); 

    // 1. Stats wala logic (Jo pehle se hai)
    var stats = _dbHandler.GetDashboardStats();
    ViewBag.TotalEmployees = stats.TotalEmployees;
    ViewBag.TotalSalary = stats.TotalSalary;
    ViewBag.ActiveTasks = stats.ActiveTasks;
    

    // 2. Data laane ka logic (Jo pehle se hai)
    var employees = _dbHandler.GetAllEmployees() ?? new List<Employee>();

    // 3. Search filter (Jo pehle se hai)
    if (!string.IsNullOrEmpty(searchString))
    {
        employees = employees.Where(e => e.Name != null && 
                    e.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    return View(employees); 
}    public IActionResult Create() { if (IsNotLoggedIn()) return RedirectToAction("Login", "Account"); return View(); }

        [HttpPost]
        public IActionResult Create(Employee emp)
        {
            if (IsNotLoggedIn()) return RedirectToAction("Login", "Account");
            if (ModelState.IsValid) { if(_dbHandler.AddEmployee(emp)) return RedirectToAction("Index"); }
            return View(emp);
        }

        public IActionResult Delete(int id)
        {
            if (IsNotLoggedIn()) return RedirectToAction("Login", "Account");
            _dbHandler.DeleteEmployee(id);
            return RedirectToAction("Index");
        }

        public IActionResult ExportToExcel()
        {
            if (IsNotLoggedIn()) return RedirectToAction("Login", "Account");
            var employees = _dbHandler.GetAllEmployees() ?? new List<Employee>();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Employees List");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Name";
                // ... (Excel logic as you had it)
                using (var stream = new MemoryStream()) {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SmartOffice_Employees.xlsx");
                }
            }
        }
        // 1. Edit Page dikhane ke liye (GET)
[HttpGet]
public IActionResult Edit(int id)
{
    var emp = _dbHandler.GetEmployeeById(id);
    if (emp == null) return NotFound();
    return View(emp); // Ye Views/Employee/Edit.cshtml ko kholega
}

// 2. Data save karne ke liye (POST)
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Edit(Employee model) // EmployeeModel ki jagah Employee kar diya agar error aa raha hai
{
    if (ModelState.IsValid)
    {
        bool isUpdated = _dbHandler.UpdateEmployee(model);
        if (isUpdated)
        {
            return RedirectToAction("Index");
        }
    }
    return View(model);
}
[HttpPost]
public IActionResult SaveNotice(string message)
{
    if (!string.IsNullOrWhiteSpace(message))
    {
        // Yahan 'Admin' ya session se naam bhej sakte ho
        _dbHandler.AddNotice(message, "Manager"); 
    }
    return RedirectToAction("Index");
}

// Index mein list bhejni hogi
}}
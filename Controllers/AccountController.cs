using Microsoft.AspNetCore.Mvc;
using SmartOfficeSystem.Data;
using SmartOfficeSystem.Models;
using Microsoft.AspNetCore.Http;

namespace SmartOfficeSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbHandler _dbHandler;

        public AccountController(ApplicationDbHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userRole = _dbHandler.GetUserRole(model.Username, model.Password);

                if (userRole != null)
                {
                    HttpContext.Session.SetString("UserSession", model.Username);
                    HttpContext.Session.SetString("UserRole", userRole);

                    // --- YAHAN FIX KIYA HAI ---
                    // Admin aur Manager dono ko Manager list pe bhejo 
                    // taaki Edit/Delete wale buttons dikhein.
                    if (userRole == "Admin" || userRole == "Manager" || userRole == "HR")
                    {
                        return RedirectToAction("Index", "Manager"); 
                    }
                    else
                    {
                        // Baaki normal employees ke liye dashboard
                        return RedirectToAction("EmployeeDashboard", "EmployeePortal");
                    }
                }
else
{
    // --- YE LINE ADD KARO (ViewBag ki jagah) ---
    ModelState.AddModelError("", "USERNAME PASSWORD GALAT HAI BESHARAM!");
}            }
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
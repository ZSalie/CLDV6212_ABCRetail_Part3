using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ABC_Retailers_Part3.Models;
using ABC_Retailers_Part3.Models.ViewModels;
using ABC_Retailers_Part3.Data;
using System.Security.Cryptography;
using System.Text;

namespace ABC_Retailers_Part3.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _context;

        public LoginController(AuthDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var hashedPassword = HashPassword(model.Password);
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == hashedPassword);

                if (user != null)
                {
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("Role", user.Role);

                    user.LastLogin = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Welcome back, {user.Username}!";

                    return user.Role == "Admin"
                        ? RedirectToAction("AdminDashboard", "Home")
                        : RedirectToAction("CustomerDashboard", "Home");
                }

                ModelState.AddModelError("", "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    Password = HashPassword(model.Password),
                    Email = model.Email,
                    Role = model.Role,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
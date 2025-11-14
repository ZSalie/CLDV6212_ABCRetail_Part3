using ABC_Retailers_Part3.Models;
using ABC_Retailers_Part3.Services;
using ABC_Retailers_Part3.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ABC_Retailers_Part3.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAzureFunctionService _functionService;

        public HomeController(IAzureFunctionService functionService)
        {
            _functionService = functionService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _functionService.GetProductsAsync();
                var customers = await _functionService.GetCustomersAsync();
                var orders = await _functionService.GetOrdersAsync();

                var viewModel = new HomeViewModel
                {
                    FeaturedProducts = products.Take(5).ToList(),
                    ProductCount = products.Count,
                    CustomerCount = customers.Count,
                    OrderCount = orders.Count
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                var viewModel = new HomeViewModel
                {
                    FeaturedProducts = new List<Product>(),
                    ProductCount = 0,
                    CustomerCount = 0,
                    OrderCount = 0
                };

                TempData["Error"] = $"Unable to load data: {ex.Message}";
                return View(viewModel);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var result = await _functionService.TestConnectionAsync();
                if (result.IsSuccess)
                {
                    TempData["Success"] = result.Message;
                }
                else
                {
                    TempData["Error"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Connection test failed: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult AdminDashboard()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("AccessDenied", "Login");
            }
            return View();
        }

        public IActionResult CustomerDashboard()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Please login to access dashboard.";
                return RedirectToAction("Login", "Login");
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
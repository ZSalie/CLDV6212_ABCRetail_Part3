using Microsoft.AspNetCore.Mvc;
using ABC_Retailers_Part3.Models;
using ABC_Retailers_Part3.Models.ViewModels;
using ABC_Retailers_Part3.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ABC_Retailers_Part3.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureFunctionService _functionService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IAzureFunctionService functionService, ILogger<OrderController> logger)
        {
            _functionService = functionService;
            _logger = logger;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var orders = await _functionService.GetOrdersAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                TempData["Error"] = "Error loading orders. Please try again.";
                return View(new List<Order>());
            }
        }

        [Authorize]
        public async Task<IActionResult> Create()
        {
            try
            {
                var customers = await _functionService.GetCustomersAsync();
                var products = await _functionService.GetProductsAsync();

                var viewModel = new OrderCreateViewModel
                {
                    Customers = customers,
                    Products = products
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order creation form");
                TempData["Error"] = "Error loading order form. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Get current user ID for order creation
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

                    var order = await _functionService.CreateOrderAsync(userId, model.ProductId, model.Quantity);
                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(MyOrders));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                ModelState.AddModelError("", $"Error creating order: {ex.Message}");
            }

            // Repopulate dropdowns if there's an error
            await PopulateDropDowns(model);
            return View(model);
        }

        private async Task PopulateDropDowns(OrderCreateViewModel model)
        {
            try
            {
                model.Customers = await _functionService.GetCustomersAsync();
                model.Products = await _functionService.GetProductsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating dropdowns");
                model.Customers = new List<Customer>();
                model.Products = new List<Product>();
            }
        }

        [Authorize]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return NotFound();
                }

                var order = await _functionService.GetOrderAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                // Check if user owns the order or is admin
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                if (!isAdmin && order.CustomerId != userId)
                {
                    TempData["Error"] = "Access denied.";
                    return RedirectToAction("AccessDenied", "Login");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details");
                TempData["Error"] = "Error loading order details. Please try again.";
                return RedirectToAction(nameof(MyOrders));
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return NotFound();
                }

                var order = await _functionService.GetOrderAsync(id);
                if (order == null)
                {
                    return NotFound();
                }
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order for edit");
                TempData["Error"] = "Error loading order. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, Order order)
        {
            try
            {
                if (id != order.Id)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    await _functionService.UpdateOrderStatusAsync(order.Id, order.Status);
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order");
                ModelState.AddModelError("", $"Error updating order: {ex.Message}");
            }
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["Error"] = "Invalid order ID.";
                    return RedirectToAction(nameof(Index));
                }

                await _functionService.DeleteOrderAsync(id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order");
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _functionService.GetProductAsync(productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        productName = product.ProductName
                        // Removed stock since your Product model doesn't have StockAvailable
                    });
                }
                return Json(new { success = false, message = "Product not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product price");
                return Json(new { success = false, message = "Error retrieving product information" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newStatus))
                {
                    return Json(new { success = false, message = "Invalid order ID or status" });
                }

                await _functionService.UpdateOrderStatusAsync(id, newStatus);
                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "Please login to view your orders.";
                    return RedirectToAction("Login", "Login");
                }

                var allOrders = await _functionService.GetOrdersAsync();
                var userOrders = allOrders.Where(o => o.CustomerId == userId).ToList();

                return View(userOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user orders");
                TempData["Error"] = "Error loading your orders. Please try again.";
                return View(new List<Order>());
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            try
            {
                var orders = await _functionService.GetOrdersAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders for management");
                TempData["Error"] = "Error loading orders. Please try again.";
                return View(new List<Order>());
            }
        }
    }
}
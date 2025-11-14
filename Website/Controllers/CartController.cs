using Microsoft.AspNetCore.Mvc;
using ABC_Retailers_Part3.Models;
using ABC_Retailers_Part3.Models.ViewModels;
using ABC_Retailers_Part3.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ABC_Retailers_Part3.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly IAzureFunctionService _functionService;
        private readonly ILogger<CartController> _logger;

        public CartController(IAzureFunctionService functionService, ILogger<CartController> logger)
        {
            _functionService = functionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var cart = await GetUserCartAsync();
                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart");
                TempData["Error"] = "Error loading your cart. Please try again.";
                return View(new CartViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(string productId, int quantity = 1)
        {
            try
            {
                if (string.IsNullOrEmpty(productId))
                {
                    TempData["Error"] = "Invalid product.";
                    return RedirectToAction("Index", "Product");
                }

                var product = await _functionService.GetProductAsync(productId);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }

                if (quantity <= 0)
                {
                    TempData["Error"] = "Quantity must be greater than 0.";
                    return RedirectToAction("Index", "Product");
                }

                var cart = await GetUserCartAsync();
                var existingItem = cart.Items.FirstOrDefault(item => item.ProductId == productId);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    cart.Items.Add(new CartItemViewModel
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName ?? "Unknown Product",
                        Price = product.Price,
                        Quantity = quantity,
                        ImageUrl = product.ImageUrl,
                        Category = product.Category
                    });
                }

                await SaveCartToSessionAsync(cart);
                TempData["Success"] = $"{product.ProductName} added to cart!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");
                TempData["Error"] = "Error adding item to cart. Please try again.";
                return RedirectToAction("Index", "Product");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCart(string productId, int quantity)
        {
            try
            {
                if (string.IsNullOrEmpty(productId))
                {
                    TempData["Error"] = "Invalid product.";
                    return RedirectToAction("Index");
                }

                var cart = await GetUserCartAsync();
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

                if (item != null)
                {
                    if (quantity <= 0)
                    {
                        cart.Items.Remove(item);
                        TempData["Success"] = "Item removed from cart.";
                    }
                    else
                    {
                        item.Quantity = quantity;
                        TempData["Success"] = "Cart updated successfully.";
                    }
                    await SaveCartToSessionAsync(cart);
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart");
                TempData["Error"] = "Error updating cart. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(string productId)
        {
            try
            {
                if (string.IsNullOrEmpty(productId))
                {
                    TempData["Error"] = "Invalid product.";
                    return RedirectToAction("Index");
                }

                var cart = await GetUserCartAsync();
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

                if (item != null)
                {
                    cart.Items.Remove(item);
                    await SaveCartToSessionAsync(cart);
                    TempData["Success"] = "Item removed from cart.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                TempData["Error"] = "Error removing item from cart. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var cart = await GetUserCartAsync();
                cart.Items.Clear();
                await SaveCartToSessionAsync(cart);
                TempData["Success"] = "Cart cleared successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["Error"] = "Error clearing cart. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var cart = await GetUserCartAsync();

                if (cart.IsEmpty)
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index");
                }

                var checkoutViewModel = new CheckoutViewModel
                {
                    Cart = cart,
                    CustomerName = User.FindFirstValue(ClaimTypes.Name) ?? HttpContext.Session.GetString("Username") ?? "Customer",
                    CustomerEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
                };

                return View(checkoutViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout");
                TempData["Error"] = "Error loading checkout. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            try
            {
                var cart = await GetUserCartAsync();
                var username = User.Identity?.Name ?? HttpContext.Session.GetString("Username") ?? "unknown";

                if (cart.IsEmpty)
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index");
                }

                // Create orders for each cart item
                foreach (var item in cart.Items)
                {
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(item.ProductId))
                    {
                        await _functionService.CreateOrderAsync(username, item.ProductId, item.Quantity);
                    }
                }

                // Clear cart after successful order
                cart.Items.Clear();
                await SaveCartToSessionAsync(cart);

                TempData["Success"] = "Order placed successfully!";
                return RedirectToAction("Confirmation", new { orderNumber = Guid.NewGuid().ToString("N")[..8].ToUpper() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order");
                TempData["Error"] = $"Error placing order: {ex.Message}";
                return View("Checkout", model);
            }
        }

        [HttpGet]
        public IActionResult Confirmation(string orderNumber)
        {
            ViewBag.OrderNumber = orderNumber ?? "N/A";
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetCartSummary()
        {
            try
            {
                var cart = await GetUserCartAsync();
                return Json(new
                {
                    totalItems = cart.TotalItems,
                    grandTotal = cart.GrandTotal.ToString("C"),
                    isEmpty = cart.IsEmpty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary");
                return Json(new { totalItems = 0, grandTotal = "$0.00", isEmpty = true });
            }
        }

        private async Task<CartViewModel> GetUserCartAsync()
        {
            var username = User.Identity?.Name ?? HttpContext.Session.GetString("Username") ?? "Guest";
            var cartJson = HttpContext.Session.GetString($"ShoppingCart_{username}");

            if (string.IsNullOrEmpty(cartJson))
            {
                return new CartViewModel();
            }

            try
            {
                var cart = JsonSerializer.Deserialize<CartViewModel>(cartJson);
                return cart ?? new CartViewModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing cart");
                return new CartViewModel();
            }
        }

        private async Task SaveCartToSessionAsync(CartViewModel cart)
        {
            var username = User.Identity?.Name ?? HttpContext.Session.GetString("Username") ?? "Guest";
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString($"ShoppingCart_{username}", cartJson);
        }
    }
}
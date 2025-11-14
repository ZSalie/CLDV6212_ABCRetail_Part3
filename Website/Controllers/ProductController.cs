using Microsoft.AspNetCore.Mvc;
using ABC_Retailers_Part3.Models;
using ABC_Retailers_Part3.Services;
using Microsoft.Extensions.Logging;

namespace ABC_Retailers_Part3.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureFunctionService _functionService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IAzureFunctionService functionService, ILogger<ProductController> logger)
        {
            _functionService = functionService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _functionService.GetProductsAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["Error"] = "Error loading products. Please try again.";
                return View(new List<Product>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            try
            {
                // Parse price as decimal instead of double
                if (Request.Form.TryGetValue("Price", out var priceFormValue))
                {
                    _logger.LogInformation("Raw price from form: '{PriceFormValue}'", priceFormValue.ToString());
                    if (decimal.TryParse(priceFormValue, out var parsedPrice))
                    {
                        product.Price = parsedPrice;
                        _logger.LogInformation("Successfully parsed price: {Price}", parsedPrice);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse price: {PriceFormValue}", priceFormValue.ToString());
                        ModelState.AddModelError("Price", "Please enter a valid price.");
                    }
                }

                // Set CreatedAt to current UTC time
                product.CreatedAt = DateTime.UtcNow;

                _logger.LogInformation("Final product price: {Price}", product.Price);

                if (ModelState.IsValid)
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price must be greater than $0.00");
                        return View(product);
                    }

                    var createdProduct = await _functionService.CreateProductAsync(product, imageFile);
                    TempData["Success"] = $"{createdProduct.ProductName} created successfully with price {createdProduct.Price:C}!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                ModelState.AddModelError("", $"Error creating product: {ex.Message}");
            }

            return View(product);
        }

        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return NotFound();
                }

                var product = await _functionService.GetProductAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product for edit");
                TempData["Error"] = "Error loading product. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Product product, IFormFile? imageFile)
        {
            try
            {
                if (id != product.ProductId)
                {
                    return NotFound();
                }

                // Parse price as decimal instead of double
                if (Request.Form.TryGetValue("Price", out var priceFormValue))
                {
                    if (decimal.TryParse(priceFormValue, out var parsedPrice))
                    {
                        product.Price = parsedPrice;
                        _logger.LogInformation("Edit: Successfully parsed price: {Price}", parsedPrice);
                    }
                    else
                    {
                        ModelState.AddModelError("Price", "Please enter a valid price.");
                    }
                }

                if (ModelState.IsValid)
                {
                    var updatedProduct = await _functionService.UpdateProductAsync(id, product, imageFile);
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                ModelState.AddModelError("", $"Error updating product: {ex.Message}");
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["Error"] = "Invalid product ID.";
                    return RedirectToAction(nameof(Index));
                }

                await _functionService.DeleteProductAsync(id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return NotFound();
                }

                var product = await _functionService.GetProductAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details");
                TempData["Error"] = "Error loading product details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
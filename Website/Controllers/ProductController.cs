using Microsoft.AspNetCore.Mvc;
using ABC_Retailers_Part3.Models;
using ABC_Retailers_Part3.Services;
using Microsoft.Extensions.Logging;

namespace ABC_Retailers_Part3.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureFunctionService _functionService;
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IAzureFunctionService functionService,
            IAzureStorageService storageService,
            ILogger<ProductController> logger)
        {
            _functionService = functionService;
            _storageService = storageService;
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
                if (ModelState.IsValid)
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price must be greater than $0.00");
                        return View(product);
                    }

                    // Generate ProductId if empty
                    if (string.IsNullOrEmpty(product.ProductId))
                    {
                        product.ProductId = Guid.NewGuid().ToString();
                    }

                    // Handle image upload to Azure Storage
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        try
                        {
                            var imageUrl = await _storageService.UploadProductImageAsync(imageFile, product.ProductId);
                            product.ImageUrl = imageUrl;
                            _logger.LogInformation("Image uploaded successfully: {ImageUrl}", imageUrl);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to upload image");
                            ModelState.AddModelError("", "Failed to upload product image. Please try again.");
                            return View(product);
                        }
                    }

                    product.CreatedAt = DateTime.UtcNow;

                    // Create product via Azure Function
                    var createdProduct = await _functionService.CreateProductAsync(product, null);
                    TempData["Success"] = $"{createdProduct.ProductName} created successfully!";
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

                if (ModelState.IsValid)
                {
                    // Handle new image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        try
                        {
                            // Delete old image if exists
                            if (!string.IsNullOrEmpty(product.ImageUrl))
                            {
                                await _storageService.DeleteProductImageAsync(product.ImageUrl);
                            }

                            // Upload new image
                            var imageUrl = await _storageService.UploadProductImageAsync(imageFile, product.ProductId);
                            product.ImageUrl = imageUrl;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to upload image");
                            ModelState.AddModelError("", "Failed to upload product image. Please try again.");
                            return View(product);
                        }
                    }

                    var updatedProduct = await _functionService.UpdateProductAsync(id, product, null);
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

                // Get product first to delete associated image
                var product = await _functionService.GetProductAsync(id);
                if (product != null && !string.IsNullOrEmpty(product.ImageUrl))
                {
                    await _storageService.DeleteProductImageAsync(product.ImageUrl);
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
using ABC_Retailers_Part3.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ABC_Retailers_Part3.Services
{
    public class AzureFunctionService : IAzureFunctionService
    {
        private readonly HttpClient _http;
        private readonly ILogger<AzureFunctionService> _logger;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        // Centralize your Function routes here
        private const string CustomersRoute = "customers";
        private const string ProductsRoute = "products";
        private const string OrdersRoute = "orders";
        private const string UploadRoute = "uploads/proof-of-payment";

        public AzureFunctionService(HttpClient http, ILogger<AzureFunctionService> logger)
        {
            _http = http;
            _logger = logger;

            // Validate BaseAddress
            if (_http.BaseAddress == null)
            {
                _logger.LogError("HttpClient BaseAddress is not set!");
                throw new InvalidOperationException("HttpClient BaseAddress must be configured");
            }

            _logger.LogInformation("AzureFunctionService initialized with base URL: {BaseUrl}", _http.BaseAddress);
        }

        // ______ Helpers ______
        private static HttpContent JsonBody(object obj)
            => new StringContent(JsonSerializer.Serialize(obj, _json), Encoding.UTF8, "application/json");

        private async Task<T> ReadJsonAsync<T>(HttpResponseMessage resp)
        {
            if (!resp.IsSuccessStatusCode)
            {
                var errorContent = await resp.Content.ReadAsStringAsync();
                _logger.LogError("HTTP {StatusCode} error: {ErrorContent}", resp.StatusCode, errorContent);
                throw new HttpRequestException($"HTTP {(int)resp.StatusCode} - {resp.StatusCode}: {errorContent}");
            }

            var stream = await resp.Content.ReadAsStreamAsync();
            var data = await JsonSerializer.DeserializeAsync<T>(stream, _json);
            return data ?? throw new InvalidOperationException("Failed to deserialize response");
        }

        private async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> operation, int maxRetries = 2)
        {
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    var response = await operation();

                    // Log the actual URL being called for debugging
                    if (i == 0) // Only log on first attempt to avoid spam
                    {
                        _logger.LogInformation("Making request to: {RequestUrl}", response.RequestMessage?.RequestUri);
                    }

                    if (response.IsSuccessStatusCode || i == maxRetries)
                        return response;

                    _logger.LogWarning("Request failed with {StatusCode}, retry {RetryCount}/{MaxRetries}",
                        response.StatusCode, i + 1, maxRetries);
                    await Task.Delay(1000 * (i + 1)); // Exponential backoff
                }
                catch (Exception ex) when (i < maxRetries)
                {
                    _logger.LogWarning(ex, "Request failed, retry {RetryCount}/{MaxRetries}", i + 1, maxRetries);
                    await Task.Delay(1000 * (i + 1));
                }
            }

            throw new HttpRequestException("All retry attempts failed");
        }

        // ______ Customers ______
        public async Task<List<Customer>> GetCustomersAsync()
        {
            try
            {
                _logger.LogInformation("Getting customers list from: {BaseUrl}/{Route}", _http.BaseAddress, CustomersRoute);
                var response = await ExecuteWithRetryAsync(() => _http.GetAsync(CustomersRoute));
                return await ReadJsonAsync<List<Customer>>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                return new List<Customer>();
            }
        }

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            try
            {
                _logger.LogInformation("Getting customer: {CustomerId}", id);
                var response = await ExecuteWithRetryAsync(() => _http.GetAsync($"{CustomersRoute}/{id}"));
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                return await ReadJsonAsync<Customer>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer {CustomerId}", id);
                return null;
            }
        }

        public async Task<Customer> CreateCustomerAsync(Customer c)
        {
            try
            {
                _logger.LogInformation("Creating customer: {CustomerName}", c.Name);
                var content = JsonBody(new
                {
                    name = c.Name,
                    surname = c.Surname,
                    username = c.Username,
                    email = c.Email,
                    shippingAddress = c.ShippingAddress
                });

                var response = await ExecuteWithRetryAsync(() => _http.PostAsync(CustomersRoute, content));
                return await ReadJsonAsync<Customer>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer {CustomerName}", c.Name);
                throw;
            }
        }

        public async Task<Customer> UpdateCustomerAsync(string id, Customer c)
        {
            try
            {
                _logger.LogInformation("Updating customer: {CustomerId}", id);
                var content = JsonBody(new
                {
                    name = c.Name,
                    surname = c.Surname,
                    username = c.Username,
                    email = c.Email,
                    shippingAddress = c.ShippingAddress
                });

                var response = await ExecuteWithRetryAsync(() => _http.PutAsync($"{CustomersRoute}/{id}", content));
                return await ReadJsonAsync<Customer>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {CustomerId}", id);
                throw;
            }
        }

        public async Task DeleteCustomerAsync(string id)
        {
            try
            {
                _logger.LogInformation("Deleting customer: {CustomerId}", id);
                var response = await ExecuteWithRetryAsync(() => _http.DeleteAsync($"{CustomersRoute}/{id}"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
                throw;
            }
        }

        // ______ Products ______
        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                _logger.LogInformation("Getting products list from: {BaseUrl}/{Route}", _http.BaseAddress, ProductsRoute);
                var response = await ExecuteWithRetryAsync(() => _http.GetAsync(ProductsRoute));
                return await ReadJsonAsync<List<Product>>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return new List<Product>();
            }
        }

        public async Task<Product?> GetProductAsync(string id)
        {
            try
            {
                _logger.LogInformation("Getting product: {ProductId}", id);
                var response = await ExecuteWithRetryAsync(() => _http.GetAsync($"{ProductsRoute}/{id}"));
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                return await ReadJsonAsync<Product>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ProductId}", id);
                return null;
            }
        }

        public async Task<Product> CreateProductAsync(Product p, IFormFile? imageFile)
        {
            try
            {
                _logger.LogInformation("Creating product: {ProductName}", p.ProductName);
                _logger.LogInformation("Using base address: {BaseAddress}", _http.BaseAddress);

                // First, let's test if the products endpoint exists
                var testResponse = await _http.GetAsync(ProductsRoute);
                _logger.LogInformation("Products endpoint test returned: {StatusCode}", testResponse.StatusCode);

                using var form = new MultipartFormDataContent();

                // Add basic product fields
                form.Add(new StringContent(p.ProductName ?? ""), "ProductName");
                form.Add(new StringContent(p.Description ?? ""), "Description");
                form.Add(new StringContent(p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price");
                form.Add(new StringContent(p.Category ?? ""), "Category");

                if (!string.IsNullOrWhiteSpace(p.ImageUrl))
                    form.Add(new StringContent(p.ImageUrl), "ImageUrl");

                // Handle image file upload
                if (imageFile is not null && imageFile.Length > 0)
                {
                    _logger.LogInformation("Including image file: {FileName} ({FileSize} bytes)",
                        imageFile.FileName, imageFile.Length);
                    var file = new StreamContent(imageFile.OpenReadStream());
                    file.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
                    form.Add(file, "ImageFile", imageFile.FileName);
                }
                else if (!string.IsNullOrWhiteSpace(p.ImageUrl))
                {
                    _logger.LogInformation("Using Image URL: {ImageUrl}", p.ImageUrl);
                }

                // Make the request
                var response = await ExecuteWithRetryAsync(() => _http.PostAsync(ProductsRoute, form));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create product. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Failed to create product: {response.StatusCode} - {errorContent}");
                }

                return await ReadJsonAsync<Product>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product {ProductName}", p.ProductName);
                throw new ApplicationException($"Failed to create product '{p.ProductName}'. Please check if the Azure Function endpoint exists and is accessible.", ex);
            }
        }

        public async Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile)
        {
            try
            {
                _logger.LogInformation("Updating product: {ProductId}", id);

                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(p.ProductName), "ProductName");
                form.Add(new StringContent(p.Description ?? string.Empty), "Description");
                form.Add(new StringContent(p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price");
                form.Add(new StringContent(p.Category ?? string.Empty), "Category");

                if (!string.IsNullOrWhiteSpace(p.ImageUrl))
                    form.Add(new StringContent(p.ImageUrl), "ImageUrl");

                if (imageFile is not null && imageFile.Length > 0)
                {
                    _logger.LogInformation("Including image file for update: {FileName}", imageFile.FileName);
                    var file = new StreamContent(imageFile.OpenReadStream());
                    file.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
                    form.Add(file, "ImageFile", imageFile.FileName);
                }

                var response = await ExecuteWithRetryAsync(() => _http.PutAsync($"{ProductsRoute}/{id}", form));
                return await ReadJsonAsync<Product>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                throw;
            }
        }

        public async Task DeleteProductAsync(string id)
        {
            try
            {
                _logger.LogInformation("Deleting product: {ProductId}", id);
                var response = await ExecuteWithRetryAsync(() => _http.DeleteAsync($"{ProductsRoute}/{id}"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                throw;
            }
        }

        // ______ Orders ______
        public async Task<List<Order>> GetOrdersAsync()
        {
            try
            {
                _logger.LogInformation("Getting orders list");
                var response = await ExecuteWithRetryAsync(() => _http.GetAsync(OrdersRoute));
                return await ReadJsonAsync<List<Order>>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders");
                return new List<Order>();
            }
        }

        public async Task<Order?> GetOrderAsync(string id)
        {
            try
            {
                _logger.LogInformation("Getting order: {OrderId}", id);
                var response = await ExecuteWithRetryAsync(() => _http.GetAsync($"{OrdersRoute}/{id}"));
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                return await ReadJsonAsync<Order>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderId}", id);
                return null;
            }
        }

        public async Task<Order> CreateOrderAsync(string customerId, string productId, int quantity)
        {
            try
            {
                _logger.LogInformation("Creating order - Customer: {CustomerId}, Product: {ProductId}, Quantity: {Quantity}",
                    customerId, productId, quantity);

                var payload = new { customerId, productId, quantity };
                var response = await ExecuteWithRetryAsync(() => _http.PostAsync(OrdersRoute, JsonBody(payload)));
                return await ReadJsonAsync<Order>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for customer {CustomerId}", customerId);
                throw;
            }
        }

        public async Task UpdateOrderStatusAsync(string id, string newStatus)
        {
            try
            {
                _logger.LogInformation("Updating order status: {OrderId} to {Status}", id, newStatus);

                // Convert string to OrderStatus enum for validation
                if (!Enum.TryParse<OrderStatus>(newStatus, true, out var status))
                {
                    throw new ArgumentException($"Invalid order status: {newStatus}");
                }

                var payload = new { status = status.ToString() };
                var response = await ExecuteWithRetryAsync(() => _http.PatchAsync($"{OrdersRoute}/{id}/status", JsonBody(payload)));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for {OrderId}", id);
                throw;
            }
        }

        public async Task UpdateOrderStatusAsync(string id, OrderStatus newStatus)
        {
            try
            {
                _logger.LogInformation("Updating order status: {OrderId} to {Status}", id, newStatus);
                var payload = new { status = newStatus.ToString() };
                var response = await ExecuteWithRetryAsync(() => _http.PatchAsync($"{OrdersRoute}/{id}/status", JsonBody(payload)));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for {OrderId}", id);
                throw;
            }
        }

        public async Task DeleteOrderAsync(string id)
        {
            try
            {
                _logger.LogInformation("Deleting order: {OrderId}", id);
                var response = await ExecuteWithRetryAsync(() => _http.DeleteAsync($"{OrdersRoute}/{id}"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", id);
                throw;
            }
        }

        // ______ Uploads ______
        public async Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName)
        {
            try
            {
                _logger.LogInformation("Uploading proof of payment: {FileName}", file.FileName);

                using var form = new MultipartFormDataContent();
                var sc = new StreamContent(file.OpenReadStream());
                sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                form.Add(sc, "ProofOfPayment", file.FileName);

                if (!string.IsNullOrWhiteSpace(orderId))
                    form.Add(new StringContent(orderId), "OrderId");
                if (!string.IsNullOrWhiteSpace(customerName))
                    form.Add(new StringContent(customerName), "CustomerName");

                var response = await ExecuteWithRetryAsync(() => _http.PostAsync(UploadRoute, form));
                response.EnsureSuccessStatusCode();

                var doc = await ReadJsonAsync<Dictionary<string, string>>(response);
                if (doc != null && doc.TryGetValue("fileName", out var name) && !string.IsNullOrEmpty(name))
                {
                    return name;
                }
                return file.FileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading proof of payment {FileName}", file.FileName);
                throw;
            }
        }

        // ______ Connection Testing ______
        public async Task<(bool IsSuccess, string Message)> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing connection to Azure Functions at: {BaseUrl}", _http.BaseAddress);

                // Test multiple endpoints to see which ones work
                var endpoints = new[] { "products", "customers", "orders" };
                var results = new List<string>();

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        var response = await _http.GetAsync(endpoint);
                        results.Add($"{endpoint}: {response.StatusCode}");
                        _logger.LogInformation("Endpoint {Endpoint} returned: {StatusCode}", endpoint, response.StatusCode);
                    }
                    catch (Exception ex)
                    {
                        results.Add($"{endpoint}: ERROR - {ex.Message}");
                        _logger.LogWarning(ex, "Endpoint {Endpoint} test failed", endpoint);
                    }
                }

                var message = $"Connection test results: {string.Join("; ", results)}";

                // If at least one endpoint returns success, consider it a partial success
                if (results.Any(r => r.Contains("200")))
                {
                    return (true, message);
                }
                else
                {
                    return (false, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                return (false, $"Connection test failed: {ex.Message}");
            }
        }
    }

    // Minimal PATCH extension for HttpClient
    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUrl, HttpContent content)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (requestUrl == null) throw new ArgumentNullException(nameof(requestUrl));

            return client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, requestUrl) { Content = content });
        }
    }
}
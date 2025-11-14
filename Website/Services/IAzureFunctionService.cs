using ABC_Retailers_Part3.Models;
using Microsoft.AspNetCore.Http;

namespace ABC_Retailers_Part3.Services
{
    public interface IAzureFunctionService
    {
        // Customers
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerAsync(string id);
        Task<Customer> CreateCustomerAsync(Customer c);
        Task<Customer> UpdateCustomerAsync(string id, Customer c);
        Task DeleteCustomerAsync(string id);

        // Products
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(string id);
        Task<Product> CreateProductAsync(Product p, IFormFile? imageFile);
        Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile);
        Task DeleteProductAsync(string id);

        // Orders
        Task<List<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(string id);
        Task<Order> CreateOrderAsync(string customerId, string productId, int quantity);
        Task UpdateOrderStatusAsync(string id, string newStatus);
        Task UpdateOrderStatusAsync(string id, OrderStatus newStatus);
        Task DeleteOrderAsync(string id);

        // Uploads
        Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName);

        // Connection Testing
        Task<(bool IsSuccess, string Message)> TestConnectionAsync();
    }
}
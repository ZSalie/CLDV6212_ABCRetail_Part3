// Models/Product.cs
namespace ABC_Retailers_Part3.Models
{
    public class Product
    {
        public string? ProductId { get; set; } = string.Empty;
        public string? ProductName { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ProofOfPayment { get; set; } = string.Empty;
    }
}
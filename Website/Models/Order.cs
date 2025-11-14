using System.ComponentModel.DataAnnotations;

namespace ABC_Retailers_Part3.Models
{
    public class Order
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        public string ProductId { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public double UnitPrice { get; set; }

        public double TotalPrice => UnitPrice * Quantity;

        public DateTime OrderDateUtc { get; set; } = DateTime.UtcNow;

        public OrderStatus Status { get; set; } = OrderStatus.Submitted;

        public string Username { get; set; } = string.Empty;
    }

    public enum OrderStatus
    {
        Submitted,
        Processing,
        Completed,
        Cancelled
    }
}
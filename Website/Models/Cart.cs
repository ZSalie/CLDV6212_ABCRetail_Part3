using System.ComponentModel.DataAnnotations;

namespace ABC_Retailers_Part3.Models
{
    public class Cart
    {
        [Key]
        public int CartId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public double Price { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}
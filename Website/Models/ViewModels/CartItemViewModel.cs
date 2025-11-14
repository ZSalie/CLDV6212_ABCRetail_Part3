// Models/ViewModels/CartItemViewModel.cs
namespace ABC_Retailers_Part3.Models.ViewModels
{
    public class CartItemViewModel
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => Price * Quantity;
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
    }
}
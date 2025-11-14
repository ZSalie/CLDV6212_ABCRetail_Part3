// Models/ViewModels/AddToCartViewModel.cs
namespace ABC_Retailers_Part3.Models.ViewModels
{
    public class AddToCartViewModel
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
        public int MaxQuantity { get; set; }
        public string? ReturnUrl { get; set; }
    }
}

// Models/ViewModels/UpdateCartViewModel.cs
namespace ABC_Retailers_Part3.Models.ViewModels
{
    public class UpdateCartViewModel
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}


// Models/ViewModels/CheckoutViewModel.cs
namespace ABC_Retailers_Part3.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public CartViewModel Cart { get; set; } = new CartViewModel();
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "Credit Card";
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? SpecialInstructions { get; set; }
    }
}
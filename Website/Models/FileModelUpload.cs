using System.ComponentModel.DataAnnotations;

namespace ABC_Retailers_Part3.Models
{
    public class FileUploadModel
    {
        [Required(ErrorMessage = "Please select a file")]
        [Display(Name = "Proof of Payment")]
        public IFormFile ProofOfPayment { get; set; } = null!; // Fixed: Initialize with null!

        [Display(Name = "Order ID (Optional)")]
        public string? OrderId { get; set; }

        [Display(Name = "Customer Name (Optional)")]
        public string? CustomerName { get; set; }
    }
}
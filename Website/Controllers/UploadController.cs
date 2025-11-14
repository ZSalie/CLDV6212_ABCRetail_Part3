using ABC_Retailers_Part3.Services;
using ABC_Retailers_Part3.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retailers_Part3.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureFunctionService _functionService;

        public UploadController(IAzureFunctionService functionService)
        {
            _functionService = functionService;
        }

        public IActionResult Index()
        {
            return View(new FileUploadModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                    {
                        var fileName = await _functionService.UploadProofOfPaymentAsync(
                            model.ProofOfPayment,
                            model.OrderId,
                            model.CustomerName);

                        TempData["Success"] = $"File uploaded successfully! File name: {fileName}";
                        return View(new FileUploadModel());
                    }
                    else
                    {
                        ModelState.AddModelError("ProofOfPayment", "Please select a file to upload.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                }
            }

            return View(model);
        }
    }
}
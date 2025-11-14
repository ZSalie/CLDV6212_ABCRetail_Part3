using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ABC_Retailers_Part3.Services
{
    public interface IAzureStorageService
    {
        Task<string> UploadProductImageAsync(IFormFile imageFile, string productId);
        Task<bool> DeleteProductImageAsync(string imageUrl);
        Task<string> UploadProofOfPaymentAsync(IFormFile file, string orderId, string customerName);
    }

    public class AzureStorageService : IAzureStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public AzureStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _containerName = configuration["AzureStorage:ContainerName"] ?? "product-images";

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Azure Storage connection string is not configured");
            }

            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadProductImageAsync(IFormFile imageFile, string productId)
        {
            if (imageFile == null || imageFile.Length == 0)
                return string.Empty;

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Generate unique blob name
            var fileExtension = Path.GetExtension(imageFile.FileName);
            var blobName = $"products/{productId}/{Guid.NewGuid()}{fileExtension}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Upload the file
            using var stream = imageFile.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders
            {
                ContentType = imageFile.ContentType
            });

            return blobClient.Uri.ToString();
        }

        public async Task<bool> DeleteProductImageAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return false;

            try
            {
                var uri = new Uri(imageUrl);
                var blobName = uri.AbsolutePath.TrimStart('/');

                // Remove container name from blob name
                var containerName = blobName.Split('/')[0];
                blobName = string.Join('/', blobName.Split('/').Skip(1));

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                return await blobClient.DeleteIfExistsAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> UploadProofOfPaymentAsync(IFormFile file, string orderId, string customerName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required");

            var containerClient = _blobServiceClient.GetBlobContainerClient("proof-of-payments");
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Generate unique blob name
            var fileExtension = Path.GetExtension(file.FileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var safeCustomerName = System.Text.RegularExpressions.Regex.Replace(customerName ?? "unknown", "[^a-zA-Z0-9]", "");
            var blobName = $"{safeCustomerName}/{orderId ?? "unknown"}/{timestamp}{fileExtension}";

            var blobClient = containerClient.GetBlobClient(blobName);

            // Upload the file
            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders
            {
                ContentType = file.ContentType
            });

            return blobClient.Uri.ToString();
        }
    }
}
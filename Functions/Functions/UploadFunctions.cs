using ABCRetailer.Functions.Helpers;
using Azure.Storage.Files.Shares;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace ABCRetailer.Functions.Functions;

public class UploadFunctions
{
    private readonly string _conn;
    private readonly string _proofs;
    private readonly string _share;
    private readonly string _shareDir;

    public UploadFunctions(IConfiguration cfg)
    {
        _conn = cfg["AzureStorage"] ?? throw new InvalidOperationException("AzureStorage connection string missing");
        _proofs = cfg["BLOB_PAYMENT_PROOFS"] ?? "payment-proofs";
        _share = cfg["FILESHARE_CONTRACTS"] ?? "contracts";
        _shareDir = cfg["FILESHARE_PAYMENTS"] ?? "payments";
    }

    [Function("Uploads_ProofOfPayment")]
    public async Task<HttpResponseData> Proof(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "uploads/proof-of-payment")] HttpRequestData req)
    {
        var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
        if (!contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            return HttpJson.Bad(req, "Expected multipart/form-data");

        var form = await MultipartHelper.ParseAsync(req.Body, contentType);
        var file = form.Files.FirstOrDefault(f => f.FieldName == "ProofOfPayment");
        if (file is null || file.Data.Length == 0)
            return HttpJson.Bad(req, "ProofOfPayment file is required");

        var orderId = form.Text.GetValueOrDefault("OrderId");
        var customerName = form.Text.GetValueOrDefault("CustomerName");

        // Blob
        var container = new BlobContainerClient(_conn, _proofs);
        await container.CreateIfNotExistsAsync();
        var blobName = $"{Guid.NewGuid()}-{file.FileName}";
        var blob = container.GetBlobClient(blobName);
        await using (var s = new MemoryStream(file.Data)) await blob.UploadAsync(s);


        // File share
        var share = new ShareClient(_conn, _share);
        await share.CreateIfNotExistsAsync();
        var root = share.GetRootDirectoryClient();
        var dir = root.GetSubdirectoryClient(_shareDir);
        await dir.CreateIfNotExistsAsync();

        var fileClient = dir.GetFileClient(blobName + ".txt");
        var msg = $"Uploaded: {DateTimeOffset.UtcNow}\nOrderId: {orderId}\nCustomerName: {customerName}\nBlobUrl: {blob.Uri}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(msg);
        using var ms = new MemoryStream(bytes);
        await fileClient.CreateAsync(ms.Length);
        await fileClient.UploadAsync(ms);

        return HttpJson.OK(req, new { fileName = blobName, blobUrl = blob.Uri.ToString() });
    }
}
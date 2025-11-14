using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Storage;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ABCRetailer.Functions.Functions;

public class BlobFunctions
{
    [Function("OnProductImageUploaded")]
    public void OnProductImageUploaded(
        [BlobTrigger("%BLOB_PRODUCT_IMAGES%/{name}", Connection = "AzureWebJobsStorage")] Stream blob,
        string name,
        FunctionContext ctx)
    {
        var log = ctx.GetLogger("OnProductImageUploaded");
        log.LogInformation($"Product image uploaded: {name}, size={blob.Length} bytes");
    }
}

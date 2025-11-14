using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using ABCRetailer.Functions.Entities;
using ABCRetailer.Functions.Helpers;
using ABCRetailer.Functions.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ABCRetailer.Functions.Functions;

public class ProductFunction
{
    private readonly string _conn;
    private readonly string _table;
    private readonly string _images;

    public ProductFunction(IConfiguration cfg)
    {
        _conn = cfg["AzureStorage"] ?? throw new InvalidOperationException("AzureStorage connection string missing");
        _table = cfg["TABLE_PRODUCT"] ?? "Product";
        _images = cfg["BLOB_PRODUCT_IMAGES"] ?? "product-images";
    }

    [Function("Products_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
    {
        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();
        var items = new List<ProductDto>();
        await foreach (var e in table.QueryAsync<ProductEntity>(x => x.PartitionKey == "Product"))
            items.Add(Map.ToDto(e));
        return HttpJson.OK(req, items);
    }

    [Function("Products_Get")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _table);
        try
        {
            var e = await table.GetEntityAsync<ProductEntity>("Product", id);
            return HttpJson.OK(req, Map.ToDto(e.Value));
        }
        catch
        {
            return HttpJson.NotFound(req, "Product not found");
        }
    }

    [Function("Products_Create")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req)
    {
        string name = "", desc = "", imageUrl = "";
        double price = 0;
        int stock = 0;

        var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";

        if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            var form = await MultipartHelper.ParseAsync(req.Body, contentType);
            name = form.Text.GetValueOrDefault("ProductName") ?? "";
            desc = form.Text.GetValueOrDefault("Description") ?? "";
            double.TryParse(form.Text.GetValueOrDefault("Price") ?? "0", out price);
            int.TryParse(form.Text.GetValueOrDefault("StockAvailable") ?? "0", out stock);

            var file = form.Files.FirstOrDefault(f => f.FieldName == "ImageFile");
            if (file is not null && file.Data.Length > 0)
            {
                var container = new BlobContainerClient(_conn, _images);
                await container.CreateIfNotExistsAsync();
                var blob = container.GetBlobClient($"{Guid.NewGuid()}-{file.FileName}");

                // FIX: Convert byte[] to MemoryStream
                using var stream = new MemoryStream(file.Data);
                await blob.UploadAsync(stream, overwrite: false);
                imageUrl = blob.Uri.ToString();
            }
            else
                imageUrl = form.Text.GetValueOrDefault("ImageUrl") ?? "";
        }
        else
        {
            // JSON fallback
            var body = await HttpJson.ReadAsync<Dictionary<string, object>>(req) ?? new();
            name = body.TryGetValue("ProductName", out var pn) ? pn.ToString() ?? "" : "";
            desc = body.TryGetValue("Description", out var d) ? d.ToString() ?? "" : "";
            price = body.TryGetValue("Price", out var pr) ? Convert.ToDouble(pr) : 0;
            stock = body.TryGetValue("StockAvailable", out var sa) ? Convert.ToInt32(sa) : 0;
            imageUrl = body.TryGetValue("ImageUrl", out var iu) ? iu.ToString() ?? "" : "";
        }

        if (string.IsNullOrWhiteSpace(name))
            return HttpJson.Bad(req, "ProductName is required");

        var e = new ProductEntity
        {
            PartitionKey = "Product",
            RowKey = Guid.NewGuid().ToString(),
            ProductName = name,
            Description = desc,
            Price = price,
            StockAvailable = stock,
            ImageUrl = imageUrl
        };

        var table = new TableClient(_conn, _table);
        await table.AddEntityAsync(e);
        return HttpJson.Created(req, Map.ToDto(e));
    }

    [Function("Products_Update")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products/{id}")] HttpRequestData req, string id)
    {
        var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
        var table = new TableClient(_conn, _table);
        try
        {
            var resp = await table.GetEntityAsync<ProductEntity>("Product", id);
            var e = resp.Value;

            if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                var form = await MultipartHelper.ParseAsync(req.Body, contentType);
                if (form.Text.TryGetValue("ProductName", out var name)) e.ProductName = name;
                if (form.Text.TryGetValue("Description", out var desc)) e.Description = desc;
                if (form.Text.TryGetValue("Price", out var priceText) && double.TryParse(priceText, out var price)) e.Price = price;
                if (form.Text.TryGetValue("StockAvailable", out var stockText) && int.TryParse(stockText, out var stock)) e.StockAvailable = stock;
                if (form.Text.TryGetValue("ImageUrl", out var iu)) e.ImageUrl = iu;

                var file = form.Files.FirstOrDefault(f => f.FieldName == "ImageFile");
                if (file is not null && file.Data.Length > 0)
                {
                    var container = new BlobContainerClient(_conn, _images);
                    await container.CreateIfNotExistsAsync();
                    var blob = container.GetBlobClient($"{Guid.NewGuid()}-{file.FileName}");

                    // FIX: Convert byte[] to MemoryStream
                    using var stream = new MemoryStream(file.Data);
                    await blob.UploadAsync(stream, overwrite: false);
                    e.ImageUrl = blob.Uri.ToString();
                }
            }
            else
            {
                var body = await HttpJson.ReadAsync<Dictionary<string, object>>(req) ?? new();
                if (body.TryGetValue("ProductName", out var pn)) e.ProductName = pn.ToString();
                if (body.TryGetValue("Description", out var d)) e.Description = d.ToString();
                if (body.TryGetValue("Price", out var pr)) e.Price = Convert.ToDouble(pr);
                if (body.TryGetValue("StockAvailable", out var sa)) e.StockAvailable = Convert.ToInt32(sa);
                if (body.TryGetValue("ImageUrl", out var iu)) e.ImageUrl = iu.ToString();
            }

            await table.UpdateEntityAsync(e, e.ETag, TableUpdateMode.Replace);
            return HttpJson.OK(req, Map.ToDto(e));
        }
        catch
        {
            return HttpJson.NotFound(req, "Product not found");
        }
    }

    [Function("Products_Delete")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "products/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _table);
        try
        {
            await table.DeleteEntityAsync("Product", id);
            return HttpJson.NoContent(req);
        }
        catch
        {
            return HttpJson.NotFound(req, "Product not found");
        }
    }
}
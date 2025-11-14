using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace ABCRetailer.Functions.Helpers;

public static class MultipartHelper
{
    public static async Task<MultipartFormData> ParseAsync(Stream body, string contentType)
    {
        var boundary = GetBoundary(MediaTypeHeaderValue.Parse(contentType));
        var reader = new MultipartReader(boundary, body);
        var form = new MultipartFormData();

        var section = await reader.ReadNextSectionAsync();
        while (section != null)
        {
            var hasContentDisposition = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

            if (hasContentDisposition)
            {
                if (contentDisposition.DispositionType.Equals("form-data"))
                {
                    var fieldName = contentDisposition.Name.Value;

                    if (contentDisposition.FileName.HasValue)
                    {
                        // This is a file
                        var file = new FormFile
                        {
                            FieldName = fieldName,
                            FileName = contentDisposition.FileName.Value,
                            ContentType = section.ContentType,
                            Data = await ReadStreamAsync(section.Body)
                        };
                        form.Files.Add(file);
                    }
                    else
                    {
                        // This is form data
                        var value = await ReadStringAsync(section.Body);
                        form.Text[fieldName] = value;
                    }
                }
            }

            section = await reader.ReadNextSectionAsync();
        }

        return form;
    }

    private static string GetBoundary(MediaTypeHeaderValue contentType)
    {
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
        if (string.IsNullOrWhiteSpace(boundary))
            throw new InvalidDataException("Missing content-type boundary.");

        return boundary;
    }

    private static async Task<byte[]> ReadStreamAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private static async Task<string> ReadStringAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}

public class MultipartFormData
{
    public Dictionary<string, string> Text { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public List<FormFile> Files { get; } = new List<FormFile>();
}

public class FormFile
{
    public string FieldName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public Stream GetStream() => new MemoryStream(Data);
}
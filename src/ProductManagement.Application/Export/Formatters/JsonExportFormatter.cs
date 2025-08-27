using Microsoft.AspNetCore.Mvc;
using ProductManagement.Export;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ProductManagement.Export.Formatters;

/// <summary>
/// JSON export formatter implementation.
/// </summary>
/// <typeparam name="TExportDto">The DTO type for export data.</typeparam>
public class JsonExportFormatter<TExportDto> : IExportFormatter<TExportDto> where TExportDto : class
{
    public string FormatName => "json";
    public string MimeType => "application/json";
    public string FileExtension => "json";

    /// <summary>
    /// Formats data as JSON.
    /// </summary>
    public IActionResult Format(IEnumerable<TExportDto> data, string fileName, bool includeHeaders = true)
    {
        var jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var bytes = Encoding.UTF8.GetBytes(jsonString);
        var memoryStream = new MemoryStream(bytes);

        return new FileStreamResult(memoryStream, MimeType)
        {
            FileDownloadName = $"{fileName}.{FileExtension}"
        };
    }
}

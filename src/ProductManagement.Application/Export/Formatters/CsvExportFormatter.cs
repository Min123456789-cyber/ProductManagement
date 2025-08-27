using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Export;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ProductManagement.Export.Formatters;

/// <summary>
/// CSV export formatter implementation.
/// </summary>
/// <typeparam name="TExportDto">The DTO type for export data.</typeparam>
public class CsvExportFormatter<TExportDto> : IExportFormatter<TExportDto> where TExportDto : class
{
    public string FormatName => "csv";
    public string MimeType => "text/csv";
    public string FileExtension => "csv";

    /// <summary>
    /// Formats data as CSV.
    /// </summary>
    public IActionResult Format(IEnumerable<TExportDto> data, string fileName, bool includeHeaders = true)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        var properties = typeof(TExportDto).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (includeHeaders)
        {
            foreach (var property in properties)
            {
                csv.WriteField(GetDisplayName(property));
            }
            csv.NextRecord();
        }

        foreach (var item in data)
        {
            foreach (var property in properties)
            {
                var value = property.GetValue(item);
                csv.WriteField(FormatValue(value));
            }
            csv.NextRecord();
        }

        writer.Flush();
        memoryStream.Position = 0;

        return new FileStreamResult(memoryStream, MimeType)
        {
            FileDownloadName = $"{fileName}.{FileExtension}"
        };
    }

    /// <summary>
    /// Gets the display name for a property.
    /// </summary>
    private static string GetDisplayName(PropertyInfo property)
    {
        // You can add custom attributes here for display names
        return property.Name.Replace("_", " ").Replace("_", " ");
    }

    /// <summary>
    /// Formats a value for CSV export.
    /// </summary>
    private static string FormatValue(object? value)
    {
        if (value == null) return "";

        if (value is System.DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        if (value is System.DateTime nullableDateTime)
        {
            return nullableDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return value.ToString() ?? "";
    }
}

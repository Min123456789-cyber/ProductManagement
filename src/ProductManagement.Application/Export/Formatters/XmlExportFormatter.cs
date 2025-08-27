using Microsoft.AspNetCore.Mvc;
using ProductManagement.Export;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ProductManagement.Export.Formatters;

/// <summary>
/// XML export formatter implementation.
/// </summary>
/// <typeparam name="TExportDto">The DTO type for export data.</typeparam>
public class XmlExportFormatter<TExportDto> : IExportFormatter<TExportDto> where TExportDto : class
{
    public string FormatName => "xml";
    public string MimeType => "application/xml";
    public string FileExtension => "xml";

    /// <summary>
    /// Formats data as XML.
    /// </summary>
    public IActionResult Format(IEnumerable<TExportDto> data, string fileName, bool includeHeaders = true)
    {
        var xmlString = new StringBuilder();
        xmlString.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        
        var entityName = typeof(TExportDto).Name.Replace("Dto", "").Replace("Export", "");
        xmlString.AppendLine($"<{entityName}s>");

        foreach (var item in data)
        {
            xmlString.AppendLine($"  <{entityName}>");
            
            var properties = typeof(TExportDto).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var value = property.GetValue(item);
                var formattedValue = FormatValue(value);
                xmlString.AppendLine($"    <{property.Name}>{formattedValue}</{property.Name}>");
            }
            
            xmlString.AppendLine($"  </{entityName}>");
        }

        xmlString.AppendLine($"</{entityName}s>");

        var bytes = Encoding.UTF8.GetBytes(xmlString.ToString());
        var memoryStream = new MemoryStream(bytes);

        return new FileStreamResult(memoryStream, MimeType)
        {
            FileDownloadName = $"{fileName}.{FileExtension}"
        };
    }

    /// <summary>
    /// Formats a value for XML export.
    /// </summary>
    private static string FormatValue(object? value)
    {
        if (value == null) return "";

        if (value is System.DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss");
        }
        
        if (value is System.DateTime nullableDateTime)
        {
            return nullableDateTime.ToString("yyyy-MM-ddTHH:mm:ss");
        }

        return value.ToString() ?? "";
    }
}

using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using ProductManagement.Export;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ProductManagement.Export.Formatters;

/// <summary>
/// Excel export formatter implementation.
/// </summary>
/// <typeparam name="TExportDto">The DTO type for export data.</typeparam>
public class ExcelExportFormatter<TExportDto> : IExportFormatter<TExportDto> where TExportDto : class
{
    public string FormatName => "excel";
    public string MimeType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string FileExtension => "xlsx";

    /// <summary>
    /// Formats data as Excel.
    /// </summary>
    public IActionResult Format(IEnumerable<TExportDto> data, string fileName, bool includeHeaders = true)
    {
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Data");

        var properties = typeof(TExportDto).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var row = 1;

        if (includeHeaders)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                worksheet.Cells[row, i + 1].Value = GetDisplayName(properties[i]);
            }

            // Style the header row
            using (var range = worksheet.Cells[1, 1, 1, properties.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            row++;
        }

        foreach (var item in data)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                var value = properties[i].GetValue(item);
                worksheet.Cells[row, i + 1].Value = FormatValue(value);
            }
            row++;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();

        var memoryStream = new MemoryStream();
        package.SaveAs(memoryStream);
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
    /// Formats a value for Excel export.
    /// </summary>
    private static object? FormatValue(object? value)
    {
        if (value == null) return "";

        if (value is System.DateTime dateTime)
        {
            return dateTime;
        }
        
        if (value is System.DateTime nullableDateTime)
        {
            return nullableDateTime;
        }

        return value;
    }
}

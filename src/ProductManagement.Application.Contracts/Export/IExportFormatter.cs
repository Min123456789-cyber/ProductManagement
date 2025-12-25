using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace ProductManagement.Export;

/// <summary>
/// Interface for export formatters that handle different output formats.
/// </summary>
/// <typeparam name="TExportDto">The DTO type for export data.</typeparam>
public interface IExportFormatter<TExportDto> where TExportDto : class
{
    /// <summary>
    /// Gets the format name this formatter handles.
    /// </summary>
    string FormatName { get; }
    
    /// <summary>
    /// Gets the MIME type for this format.
    /// </summary>
    string MimeType { get; }
    
    /// <summary>
    /// Gets the file extension for this format.
    /// </summary>
    string FileExtension { get; }
    
    /// <summary>
    /// Formats the data for export.
    /// </summary>
    /// <param name="data">The data to format.</param>
    /// <param name="fileName">The base file name.</param>
    /// <param name="includeHeaders">Whether to include headers.</param>
    /// <returns>File result with formatted data.</returns>
    IActionResult Format(IEnumerable<TExportDto> data, string fileName, bool includeHeaders = true);
}

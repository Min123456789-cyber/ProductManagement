using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductManagement.Export;

/// <summary>
/// Generic interface for export services that can export data in different formats.
/// </summary>
/// <typeparam name="TEntity">The entity type to export.</typeparam>
/// <typeparam name="TExportDto">The DTO type for export data.</typeparam>
/// <typeparam name="TFilter">The filter type for data filtering.</typeparam>
public interface IExportService<TEntity, TExportDto, TFilter>
    where TEntity : class
    where TExportDto : class
    where TFilter : class
{
    /// <summary>
    /// Exports data in the specified format.
    /// </summary>
    /// <param name="request">Export request containing format and filter options.</param>
    /// <param name="data">The data to export.</param>
    /// <returns>File result with exported data.</returns>
    Task<IActionResult> ExportAsync(ExportRequestDto<TFilter> request, IEnumerable<TExportDto> data);
}

/// <summary>
/// Generic export request DTO that can be used for any entity.
/// </summary>
/// <typeparam name="TFilter">The filter type for data filtering.</typeparam>
public record ExportRequestDto<TFilter> where TFilter : class
{
    /// <summary>
    /// The export format (csv, excel, json, xml).
    /// </summary>
    public string Format { get; set; } = "csv";
    
    /// <summary>
    /// Optional filter for data.
    /// </summary>
    public TFilter? Filter { get; set; }
    
    /// <summary>
    /// Optional custom file name.
    /// </summary>
    public string? FileName { get; set; }
    
    /// <summary>
    /// Whether to include headers in the export.
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;
    
    /// <summary>
    /// The entity name for file naming.
    /// </summary>
    public string EntityName { get; set; } = "data";
}

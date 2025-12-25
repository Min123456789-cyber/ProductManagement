using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProductManagement.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;

namespace ProductManagement.Export;

/// <summary>
/// Generic export service implementation that can export any entity type in different formats.
/// </summary>
/// <typeparam name="TEntity">The entity type to export.</typeparam>
/// <typeparam name="TExportDto">The DTO type for export data.</typeparam>
/// <typeparam name="TFilter">The filter type for data filtering.</typeparam>
public class GenericExportService<TEntity, TExportDto, TFilter> : IExportService<TEntity, TExportDto, TFilter>
    where TEntity : class
    where TExportDto : class
    where TFilter : class
{
    private readonly ILogger<GenericExportService<TEntity, TExportDto, TFilter>> _logger;
    private readonly IEnumerable<IExportFormatter<TExportDto>> _formatters;

    public GenericExportService(
        ILogger<GenericExportService<TEntity, TExportDto, TFilter>> logger,
        IEnumerable<IExportFormatter<TExportDto>> formatters)
    {
        _logger = logger;
        _formatters = formatters;
    }

    /// <summary>
    /// Exports data in the specified format.
    /// </summary>
    /// <param name="request">Export request containing format and filter options.</param>
    /// <param name="data">The data to export.</param>
    /// <returns>File result with exported data.</returns>
    public async Task<IActionResult> ExportAsync(ExportRequestDto<TFilter> request, IEnumerable<TExportDto> data)
    {
        try
        {
            _logger.LogInformation("Starting export process for {EntityType} with format: {Format}", 
                typeof(TEntity).Name, request.Format);

            if (!data.Any())
            {
                _logger.LogWarning("No data found for export");
                throw new UserFriendlyException("No data found to export.", "404");
            }

            var fileName = string.IsNullOrWhiteSpace(request.FileName)
                ? $"{request.EntityName}_export_{DateTime.Now:yyyyMMdd_HHmmss}"
                : request.FileName;

            var formatter = _formatters.FirstOrDefault(f => 
                f.FormatName.Equals(request.Format, StringComparison.OrdinalIgnoreCase));

            if (formatter == null)
            {
                _logger.LogError("Unsupported export format: {Format}", request.Format);
                throw new UserFriendlyException($"Unsupported export format: {request.Format}", "400");
            }

            _logger.LogInformation("Exporting {Count} records in {Format} format", data.Count(), request.Format);

            return formatter.Format(data, fileName, request.IncludeHeaders);
        }
        catch (Exception ex) when (!(ex is UserFriendlyException))
        {
            _logger.LogError(ex, "Failed to export {EntityType} data. Error: {ErrorMessage}", 
                typeof(TEntity).Name, ex.Message);
            throw new UserFriendlyException("An error occurred during export.", "500");
        }
    }
}

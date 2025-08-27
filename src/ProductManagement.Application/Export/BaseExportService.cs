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
/// Base export service that provides common functionality for entity-specific export services.
/// </summary>
/// <typeparam name="TEntity">The entity type to export.</typeparam>
/// <typeparam name="TExportDto">The DTO type for export data.</typeparam>
/// <typeparam name="TFilter">The filter type for data filtering.</typeparam>
public abstract class BaseExportService<TEntity, TExportDto, TFilter>
    where TEntity : class
    where TExportDto : class
    where TFilter : class
{
    private readonly IExportService<TEntity, TExportDto, TFilter> _exportService;
    private readonly ILogger<BaseExportService<TEntity, TExportDto, TFilter>> _logger;

    protected BaseExportService(
        IExportService<TEntity, TExportDto, TFilter> exportService,
        ILogger<BaseExportService<TEntity, TExportDto, TFilter>> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Exports data in the specified format.
    /// </summary>
    /// <param name="request">Export request containing format and filter options.</param>
    /// <returns>File result with exported data.</returns>
    public async Task<IActionResult> ExportAsync(ExportRequestDto<TFilter> request)
    {
        try
        {
            _logger.LogInformation("Starting export process for {EntityType} with format: {Format}", 
                typeof(TEntity).Name, request.Format);

            // Get the data to export
            var data = await GetDataAsync(request.Filter);

            if (!data.Any())
            {
                _logger.LogWarning("No data found for export");
                throw new UserFriendlyException("No data found to export.", "404");
            }

            // Set the entity name if not provided
            if (string.IsNullOrWhiteSpace(request.EntityName))
            {
                request.EntityName = typeof(TEntity).Name.ToLowerInvariant();
            }

            return await _exportService.ExportAsync(request, data);
        }
        catch (Exception ex) when (!(ex is UserFriendlyException))
        {
            _logger.LogError(ex, "Failed to export {EntityType} data. Error: {ErrorMessage}", 
                typeof(TEntity).Name, ex.Message);
            throw new UserFriendlyException("An error occurred during export.", "500");
        }
    }

    /// <summary>
    /// Gets the data to export. This method must be implemented by derived classes.
    /// </summary>
    /// <param name="filter">Optional filter for the data.</param>
    /// <returns>The data to export.</returns>
    protected abstract Task<IEnumerable<TExportDto>> GetDataAsync(TFilter? filter);

    /// <summary>
    /// Applies filtering to the data query. This method can be overridden by derived classes.
    /// </summary>
    /// <param name="query">The base query.</param>
    /// <param name="filter">The filter to apply.</param>
    /// <returns>The filtered query.</returns>
    protected virtual IQueryable<TExportDto> ApplyFilter(IQueryable<TExportDto> query, TFilter? filter)
    {
        // Default implementation - derived classes can override this
        return query;
    }
}

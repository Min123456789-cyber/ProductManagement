using Microsoft.AspNetCore.Mvc;
using ProductManagement.Teachers;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace ProductManagement.Controllers;

[Route("api/teachers")]
public class TeacherController : ProductManagementController
{
    private readonly ITeacherAppService _teacherAppService;

    public TeacherController(ITeacherAppService teacherAppService)
    {
        _teacherAppService = teacherAppService;
    }

    /// <summary>
    /// Exports teacher data in different formats (CSV, Excel, JSON, XML).
    /// </summary>
    /// <param name="request">Export request containing format and filter options.</param>
    /// <returns>File result with exported data.</returns>
    [HttpPost("export")]
    public async Task<IActionResult> ExportAsync([FromBody] TeacherExportRequestDto request)
    {
        return await _teacherAppService.ExportAsync(request);
    }

    /// <summary>
    /// Exports teacher data in CSV format.
    /// </summary>
    /// <param name="searchKeyword">Optional search keyword to filter teachers.</param>
    /// <param name="fileName">Optional custom file name.</param>
    /// <returns>CSV file with teacher data.</returns>
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsvAsync([FromQuery] string? searchKeyword = null, [FromQuery] string? fileName = null)
    {
        var request = new TeacherExportRequestDto
        {
            Format = "csv",
            FileName = fileName,
            Filter = !string.IsNullOrWhiteSpace(searchKeyword) ? new TeacherFilter { SearchKeyword = searchKeyword } : null
        };

        return await _teacherAppService.ExportAsync(request);
    }

    /// <summary>
    /// Exports teacher data in Excel format.
    /// </summary>
    /// <param name="searchKeyword">Optional search keyword to filter teachers.</param>
    /// <param name="fileName">Optional custom file name.</param>
    /// <returns>Excel file with teacher data.</returns>
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcelAsync([FromQuery] string? searchKeyword = null, [FromQuery] string? fileName = null)
    {
        var request = new TeacherExportRequestDto
        {
            Format = "excel",
            FileName = fileName,
            Filter = !string.IsNullOrWhiteSpace(searchKeyword) ? new TeacherFilter { SearchKeyword = searchKeyword } : null
        };

        return await _teacherAppService.ExportAsync(request);
    }

    /// <summary>
    /// Exports teacher data in JSON format.
    /// </summary>
    /// <param name="searchKeyword">Optional search keyword to filter teachers.</param>
    /// <param name="fileName">Optional custom file name.</param>
    /// <returns>JSON file with teacher data.</returns>
    [HttpGet("export/json")]
    public async Task<IActionResult> ExportJsonAsync([FromQuery] string? searchKeyword = null, [FromQuery] string? fileName = null)
    {
        var request = new TeacherExportRequestDto
        {
            Format = "json",
            FileName = fileName,
            Filter = !string.IsNullOrWhiteSpace(searchKeyword) ? new TeacherFilter { SearchKeyword = searchKeyword } : null
        };

        return await _teacherAppService.ExportAsync(request);
    }

    /// <summary>
    /// Exports teacher data in XML format.
    /// </summary>
    /// <param name="searchKeyword">Optional search keyword to filter teachers.</param>
    /// <param name="fileName">Optional custom file name.</param>
    /// <returns>XML file with teacher data.</returns>
    [HttpGet("export/xml")]
    public async Task<IActionResult> ExportXmlAsync([FromQuery] string? searchKeyword = null, [FromQuery] string? fileName = null)
    {
        var request = new TeacherExportRequestDto
        {
            Format = "xml",
            FileName = fileName,
            Filter = !string.IsNullOrWhiteSpace(searchKeyword) ? new TeacherFilter { SearchKeyword = searchKeyword } : null
        };

        return await _teacherAppService.ExportAsync(request);
    }
}

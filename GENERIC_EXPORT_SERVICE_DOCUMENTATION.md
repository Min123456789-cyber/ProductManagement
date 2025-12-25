# Generic Export Service Documentation

This document describes the generic export service architecture that provides reusable export functionality across different entities in the application.

## Overview

The generic export service is designed to provide a flexible, extensible, and reusable export system that can work with any entity type. It follows the Strategy pattern and uses dependency injection for maximum flexibility.

## Architecture

### Core Components

#### 1. IExportService<TEntity, TExportDto, TFilter>
Generic interface for export services that can export data in different formats.

```csharp
public interface IExportService<TEntity, TExportDto, TFilter>
    where TEntity : class
    where TExportDto : class
    where TFilter : class
{
    Task<IActionResult> ExportAsync(ExportRequestDto<TFilter> request, IEnumerable<TExportDto> data);
}
```

#### 2. IExportFormatter<TExportDto>
Interface for export formatters that handle different output formats.

```csharp
public interface IExportFormatter<TExportDto> where TExportDto : class
{
    string FormatName { get; }
    string MimeType { get; }
    string FileExtension { get; }
    IActionResult Format(IEnumerable<TExportDto> data, string fileName, bool includeHeaders = true);
}
```

#### 3. ExportRequestDto<TFilter>
Generic export request DTO that can be used for any entity.

```csharp
public record ExportRequestDto<TFilter> where TFilter : class
{
    public string Format { get; set; } = "csv";
    public TFilter? Filter { get; set; }
    public string? FileName { get; set; }
    public bool IncludeHeaders { get; set; } = true;
    public string EntityName { get; set; } = "data";
}
```

### Implementation Classes

#### 1. GenericExportService<TEntity, TExportDto, TFilter>
Main implementation that orchestrates the export process.

#### 2. BaseExportService<TEntity, TExportDto, TFilter>
Abstract base class that provides common functionality for entity-specific export services.

#### 3. Export Formatters
- `CsvExportFormatter<TExportDto>` - Exports data as CSV
- `ExcelExportFormatter<TExportDto>` - Exports data as Excel (XLSX)
- `JsonExportFormatter<TExportDto>` - Exports data as JSON
- `XmlExportFormatter<TExportDto>` - Exports data as XML

## Usage Examples

### 1. Creating an Entity-Specific Export Service

```csharp
public class TeacherExportService : BaseExportService<Teacher, TeacherExportDto, TeacherFilter>
{
    private readonly IRepository<Teacher, Guid> _teacherRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;

    public TeacherExportService(
        IExportService<Teacher, TeacherExportDto, TeacherFilter> exportService,
        IRepository<Teacher, Guid> teacherRepository,
        IRepository<Department, Guid> departmentRepository,
        ILogger<TeacherExportService> logger)
        : base(exportService, logger)
    {
        _teacherRepository = teacherRepository;
        _departmentRepository = departmentRepository;
    }

    protected override async Task<IEnumerable<TeacherExportDto>> GetDataAsync(TeacherFilter? filter)
    {
        var teachers = await _teacherRepository.GetQueryableAsync();
        var departments = await _departmentRepository.GetQueryableAsync();

        var query = from t in teachers
                    join d in departments on t.DepartmentId equals d.Id
                    select new TeacherExportDto
                    {
                        Id = t.Id,
                        FirstName = t.FirstName,
                        LastName = t.LastName,
                        Email = t.Email,
                        DepartmentName = d.Name,
                        // ... other properties
                    };

        // Apply filter if provided
        if (filter != null && !string.IsNullOrWhiteSpace(filter.SearchKeyword))
        {
            var searchKeyword = filter.SearchKeyword.Trim().ToLower();
            query = query.Where(x =>
                x.FirstName.ToLower().Contains(searchKeyword) ||
                x.LastName.ToLower().Contains(searchKeyword) ||
                x.Email.ToLower().Contains(searchKeyword)
            );
        }

        return await query.ToListAsync();
    }
}
```

### 2. Using the Export Service in an AppService

```csharp
public class TeacherAppService : ApplicationService, ITeacherAppService
{
    public async Task<IActionResult> ExportAsync(TeacherExportRequestDto request)
    {
        var exportRequest = new ExportRequestDto<TeacherFilter>
        {
            Format = request.Format,
            FileName = request.FileName,
            IncludeHeaders = request.IncludeHeaders,
            Filter = request.Filter,
            EntityName = "teachers"
        };

        var exportService = LazyServiceProvider.LazyGetRequiredService<TeacherExportService>();
        return await exportService.ExportAsync(exportRequest);
    }
}
```

### 3. Creating a Custom Export Formatter

```csharp
public class PdfExportFormatter<TExportDto> : IExportFormatter<TExportDto> where TExportDto : class
{
    public string FormatName => "pdf";
    public string MimeType => "application/pdf";
    public string FileExtension => "pdf";

    public IActionResult Format(IEnumerable<TExportDto> data, string fileName, bool includeHeaders = true)
    {
        // Implement PDF generation logic
        // You can use libraries like iTextSharp, PdfSharp, or similar
        
        var pdfBytes = GeneratePdf(data, includeHeaders);
        var memoryStream = new MemoryStream(pdfBytes);

        return new FileStreamResult(memoryStream, MimeType)
        {
            FileDownloadName = $"{fileName}.{FileExtension}"
        };
    }

    private byte[] GeneratePdf(IEnumerable<TExportDto> data, bool includeHeaders)
    {
        // PDF generation implementation
        throw new NotImplementedException();
    }
}
```

## Dependency Injection Setup

Register the services in your module:

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    // Register export formatters
    context.Services.AddTransient(typeof(CsvExportFormatter<>));
    context.Services.AddTransient(typeof(ExcelExportFormatter<>));
    context.Services.AddTransient(typeof(JsonExportFormatter<>));
    context.Services.AddTransient(typeof(XmlExportFormatter<>));

    // Register generic export services
    context.Services.AddTransient(typeof(IExportService<,,>), typeof(GenericExportService<,,>));
    context.Services.AddTransient(typeof(BaseExportService<,,>));

    // Register specific export services
    context.Services.AddTransient<TeacherExportService>();
    context.Services.AddTransient<ProductExportService>();
    // ... other entity export services
}
```

## Supported Formats

### 1. CSV (Comma-Separated Values)
- **Format Name**: `csv`
- **MIME Type**: `text/csv`
- **File Extension**: `.csv`
- **Features**: UTF-8 encoding, comma delimiter, automatic header generation

### 2. Excel (XLSX)
- **Format Name**: `excel`
- **MIME Type**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **File Extension**: `.xlsx`
- **Features**: Formatted headers, auto-fit columns, styling

### 3. JSON (JavaScript Object Notation)
- **Format Name**: `json`
- **MIME Type**: `application/json`
- **File Extension**: `.json`
- **Features**: Indented formatting, camelCase property naming

### 4. XML (Extensible Markup Language)
- **Format Name**: `xml`
- **MIME Type**: `application/xml`
- **File Extension**: `.xml`
- **Features**: Proper XML declaration, structured output

## Extending the System

### Adding New Export Formats

1. Create a new formatter class implementing `IExportFormatter<TExportDto>`
2. Register it in the DI container
3. The format will be automatically available to all export services

### Adding Custom Filtering Logic

Override the `ApplyFilter` method in your entity-specific export service:

```csharp
protected override IQueryable<TeacherExportDto> ApplyFilter(IQueryable<TeacherExportDto> query, TeacherFilter? filter)
{
    if (filter?.DepartmentId != null)
    {
        query = query.Where(x => x.DepartmentId == filter.DepartmentId);
    }
    
    return base.ApplyFilter(query, filter);
}
```

### Adding Custom Property Display Names

Create a custom attribute and modify the formatters to use it:

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class ExportDisplayNameAttribute : Attribute
{
    public string DisplayName { get; }
    
    public ExportDisplayNameAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}

// Usage in DTO
public class TeacherExportDto
{
    [ExportDisplayName("Teacher ID")]
    public Guid Id { get; set; }
    
    [ExportDisplayName("First Name")]
    public string FirstName { get; set; }
}
```

## Best Practices

### 1. Performance Considerations
- Use `IQueryable` for data retrieval to enable deferred execution
- Implement pagination for large datasets
- Consider caching for frequently exported data

### 2. Error Handling
- Always validate input parameters
- Provide meaningful error messages
- Log errors for debugging

### 3. Security
- Validate file names to prevent path traversal attacks
- Implement proper authorization checks
- Sanitize data before export

### 4. Testing
- Unit test individual formatters
- Integration test the complete export flow
- Test with various data types and edge cases

## API Endpoints

The generic export service can be exposed through REST API endpoints:

```csharp
[Route("api/teachers")]
public class TeacherController : ProductManagementController
{
    [HttpPost("export")]
    public async Task<IActionResult> ExportAsync([FromBody] TeacherExportRequestDto request)
    {
        return await _teacherAppService.ExportAsync(request);
    }

    [HttpGet("export/{format}")]
    public async Task<IActionResult> ExportFormatAsync(
        string format, 
        [FromQuery] string? searchKeyword = null, 
        [FromQuery] string? fileName = null)
    {
        var request = new TeacherExportRequestDto
        {
            Format = format,
            FileName = fileName,
            Filter = !string.IsNullOrWhiteSpace(searchKeyword) 
                ? new TeacherFilter { SearchKeyword = searchKeyword } 
                : null
        };

        return await _teacherAppService.ExportAsync(request);
    }
}
```

## Migration from Entity-Specific Export

To migrate from entity-specific export implementations to the generic service:

1. Create an entity-specific export service inheriting from `BaseExportService`
2. Implement the `GetDataAsync` method
3. Update the app service to use the new export service
4. Remove the old export methods
5. Update the DI registration

This architecture provides a clean, maintainable, and extensible solution for data export functionality across your application.

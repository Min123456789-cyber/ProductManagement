using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Teachers;

public record TeacherExportRequestDto
{
    [Required(ErrorMessage = "Export format is required.")]
    public string Format { get; set; } = "csv"; // csv, excel, json, xml
    
    public TeacherFilter? Filter { get; set; }
    
    public string? FileName { get; set; }
    
    public bool IncludeHeaders { get; set; } = true;
}

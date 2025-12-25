using System;
using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Teachers;

public class CreateUpdateTeacherDto
{
    [Required(ErrorMessage = "First Name is required.")]
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Last Name is required.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile Number is required.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    public Guid DepartmentId { get; set; }
}

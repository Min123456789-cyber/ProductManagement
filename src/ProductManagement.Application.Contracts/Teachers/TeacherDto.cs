using System;

namespace ProductManagement.Teachers;

public class TeacherDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } 
}

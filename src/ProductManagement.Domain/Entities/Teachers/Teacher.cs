using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace ProductManagement.Entities.Teachers;

public class Teacher : AuditedAggregateRoot<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
}

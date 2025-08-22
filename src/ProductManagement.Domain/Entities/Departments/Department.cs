using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace ProductManagement.Entities.Departments;

public class Department : AuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
}

using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace ProductManagement.Entities.Category;

public class Category : AuditedEntity<Guid>
{
    public string Name { get; set; }
}

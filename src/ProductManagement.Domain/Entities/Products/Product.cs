using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace ProductManagement.Entities.Products;

public class Product : AuditedEntity<Guid>
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int StockQuantity { get; set; }
    public Guid? CategoryId { get; set; }
}

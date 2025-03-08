using Microsoft.EntityFrameworkCore.Diagnostics;
using ProductManagement.Entities.Products;
using System;

namespace ProductManagement.Products;

public class ProductCreatedEvent
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; }
}

using Microsoft.EntityFrameworkCore.Diagnostics;
using System;

namespace ProductManagement.Products;

public class ProductPriceChangedEvent
{
    public Guid ProductId { get; set; }
    public decimal NewPrice { get; set; }
}

public class ProductStockChangedEvent
{
    public Guid ProductId { get; set; }
    public int NewStockQuantity { get; set; }
}

using ProductManagement.Products;
using System.Threading.Tasks;
using System;
using Volo.Abp.EventBus;
using Microsoft.Extensions.Logging;

namespace ProductManagement.EventHandler;

public class ProductStockChangedEventHandler(ILogger<ProductPriceChangedEventHandler> logger) : ILocalEventHandler<ProductStockChangedEvent>
{
    public async Task HandleEventAsync(ProductStockChangedEvent eventData)
    {
        // Handle the stock change event
        logger.LogInformation($"Product {eventData.ProductId} stock changed to {eventData.NewStockQuantity}");

        // Example: trigger notifications or update other related services.
        await Task.CompletedTask;
    }
}

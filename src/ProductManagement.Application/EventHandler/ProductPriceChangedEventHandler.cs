using Microsoft.Extensions.Logging;
using ProductManagement.Products;
using System.Threading.Tasks;
using Volo.Abp.EventBus;

namespace ProductManagement.EventHandler;

public class ProductPriceChangedEventHandler(ILogger<ProductPriceChangedEventHandler> logger) : ILocalEventHandler<ProductPriceChangedEvent>
{
    public async Task HandleEventAsync(ProductPriceChangedEvent eventData)
    {
        // Handle the price change event, such as updating caches, notifying users, or triggering workflows.
        logger.LogInformation($"Product {eventData.ProductId} price changed to {eventData.NewPrice}");

        // Example: update a cache, notify other services, etc.
        await Task.CompletedTask;
    }
}

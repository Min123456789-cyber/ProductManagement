using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductManagement.Dtos;
using ProductManagement.Entities.Category;
using ProductManagement.Entities.Products;
using ProductManagement.Products;
using ProductManagement.Responses;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace ProductManagement.AppServices.Products;

public class ProductAppService : ApplicationService, IProductAppService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductAppService> _logger;
    private readonly ILocalEventBus _localEventBus;

    public ProductAppService(
        IRepository<Product, Guid> productRepository,
        IMapper mapper,
        ILogger<ProductAppService> logger,
        IRepository<Category, Guid> categoryRepository,
        ILocalEventBus localEventBus)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _logger = logger;
        _categoryRepository = categoryRepository;
        _localEventBus = localEventBus;
    }

    public async Task<ResponseDataDto<object>> CreateAsync(CreateUpdateProductDto input)
    {
        try
        {
            _logger.LogInformation("Creating new product with name: {ProductName}", input.Name);

            var product = _mapper.Map<CreateUpdateProductDto, Product>(input);
            await _productRepository.InsertAsync(product);

            _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);

            var query = _mapper.Map<Product, ProductDto>(product);

            var category = await _categoryRepository.FirstOrDefaultAsync(x => x.Id == query.CategoryId);

            var result = new ProductDto
            {
                Id = query.Id,
                Name = query.Name,
                Description = query.Description,
                ImageUrl = query.ImageUrl,
                Price = query.Price,
                StockQuantity = query.StockQuantity,
                CategoryId = query.CategoryId,
                CategoryName = category.Name
            };

            return new ResponseDataDto<object>
            {
                Success = true,
                Code = 200,
                Message = "Product created successfully.",
                Data = result
            };
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating product with name {ProductName}: {Message}", 
                input.Name, ex.Message);
            throw new UserFriendlyException("An error occurred while creating the product.", "500");
        }
    }

    public async Task<ResponseDataDto<object>> UpdateAsync([Required(ErrorMessage = "Id is required.")] Guid id, CreateUpdateProductDto input)
    {
        try
        {
            _logger.LogInformation("Updating product with ID: {ProductId}", id);

            var product = await _productRepository.GetAsync(id);
            _mapper.Map(input, product);
            await _productRepository.UpdateAsync(product);

            _logger.LogInformation("Product updated successfully with ID: {ProductId}", id);

            var result = _mapper.Map<Product, ProductDto>(product);

            // Publish ProductPriceChanged event
            await _localEventBus.PublishAsync(new ProductPriceChangedEvent
            {
                ProductId = result.Id,
                NewPrice = result.Price
            });

            // Publish ProductStockChanged event
            await _localEventBus.PublishAsync(new ProductStockChangedEvent
            {
                ProductId = result.Id,
                NewStockQuantity = result.StockQuantity
            });

            return new ResponseDataDto<object>
            {
                Success = true,
                Code = 200,
                Message = "Product updated successfully.",
                Data = result
            };
        }
        catch (UserFriendlyException ex)
        {
            _logger.LogWarning(ex, "User friendly exception occurred while updating product: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating product with ID {ProductId}: {Message}", 
                id, ex.Message);
            throw new UserFriendlyException("An error occurred while updating the product.", "500");
        }
    }

    public async Task<ResponseDataDto<object>> DeleteAsync([Required(ErrorMessage = "Id is required.")] Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting product with ID: {ProductId}", id);

            await _productRepository.DeleteAsync(id);

            _logger.LogInformation("Product deleted successfully with ID: {ProductId}", id);

            return new ResponseDataDto<object>
            {
                Success = true,
                Code = 200,
                Message = "Product deleted successfully.",
                Data = null
            };
        }
        catch (UserFriendlyException ex)
        {
            _logger.LogWarning(ex, "User friendly exception occurred while deleting product: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting product with ID {ProductId}: {Message}", 
                id, ex.Message);
            throw new UserFriendlyException("An error occurred while deleting the product.", "500");
        }
    }

    public async Task<ResponseDataDto<ProductDto>> GetAsync([Required(ErrorMessage = "Id is required.")] Guid id)
    {
        try
        {
            _logger.LogInformation("Retrieving product with ID: {ProductId}", id);

            var product = await _productRepository.GetAsync(id);

            var products = await _productRepository.GetQueryableAsync();
            var categories = await _categoryRepository.GetQueryableAsync();

            var result = await (from p in products
                          join c in categories on p.CategoryId equals c.Id
                          select new ProductDto
                          {
                              Id = p.Id,
                              Name = p.Name,
                              Description = p.Description,
                              Price = p.Price,
                              ImageUrl = p.ImageUrl,
                              StockQuantity = p.StockQuantity,
                              CategoryId = p.CategoryId,
                              CategoryName = c.Name
                          }).FirstOrDefaultAsync();

            _logger.LogInformation("Product retrieved successfully with ID: {ProductId}", id);

            return new ResponseDataDto<ProductDto>
            {
                Success = true,
                Code = 200,
                Message = "Product retrieved successfully.",
                Data = result
            };
        }
        catch (UserFriendlyException ex)
        {
            _logger.LogWarning(ex, "User friendly exception occurred while retrieving product: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving product with ID {ProductId}: {Message}", 
                id, ex.Message);
            throw new UserFriendlyException("An error occurred while retrieving the product.", "500");
        }
    }

    public async Task<ResponseDataDto<PagedResultDto<ProductDto>>> GetListAsync(PagedAndSortedResultRequestDto input, ProductFilter filter)
    {
        try
        {
            _logger.LogInformation("ProductAppService - GetListAsync: Started");

            if (input.Sorting.IsNullOrWhiteSpace())
            {
                input.Sorting = "Name";
            }
            filter.SearchKeyword = filter.SearchKeyword?.Trim()?.ToLower();

            var products = await _productRepository.GetQueryableAsync();
            var categories = await _categoryRepository.GetQueryableAsync();

            var query = (from p in products
                         join c in categories on p.CategoryId equals c.Id
                         select new ProductDto
                         {
                             Id = p.Id,
                             Name = p.Name,
                             Description = p.Description,
                             Price = p.Price,
                             ImageUrl = p.ImageUrl,
                             StockQuantity = p.StockQuantity,
                             CategoryId = p.CategoryId,
                             CategoryName = c.Name
                         })
                        .WhereIf(!string.IsNullOrWhiteSpace(filter.SearchKeyword),
                            x => x.Name.ToLower().Contains(filter.SearchKeyword) ||
                                 x.Description.ToLower().Contains(filter.SearchKeyword) ||
                                 x.CategoryName.ToLower().Contains(filter.SearchKeyword)
                        );

            var items = await AsyncExecuter.ToListAsync(
                query
                .OrderBy(input.Sorting)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
            );

            var totalCount = await AsyncExecuter.CountAsync(query);
            
            _logger.LogInformation("Retrieved {Count} products successfully", items.Count);

            var result = new PagedResultDto<ProductDto>(totalCount, items);
            return new ResponseDataDto<PagedResultDto<ProductDto>>
            {
                Success = true,
                Code = 200,
                Message = "Products retrieved successfully.",
                Data = result
            };
        }
        catch (UserFriendlyException ex)
        {
            _logger.LogWarning(ex, "User friendly exception occurred while retrieving products: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving products: {Message}", ex.Message);
            throw new UserFriendlyException("An error occurred while retrieving the products.", "500");
        }
    }
    
    public async Task<ResponseDataDto<DropDownDto[]>> GetCategoriesAsync()
    {
        try
        {
            _logger.LogInformation("ProductAppService - GetCategoriesAsync: Started");

            var categories = await _categoryRepository.GetQueryableAsync();

            var result = await AsyncExecuter.ToArrayAsync(
                categories
                .Select(x => new DropDownDto
                {
                    Value = x.Id.ToString(),
                    Name = x.Name 
                }).OrderBy(x => x.Name)
            );

            return new ResponseDataDto<DropDownDto[]>
            {
                Code = 200,
                Success = true,
                Message = "Data retrieved successfully.",
                Data = result
            };
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving products: {Message}", ex.Message);
            throw new UserFriendlyException("An error occurred while retrieving the products.", "500");
        }
    }
}


using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductManagement.Constants;
using ProductManagement.Entities.Category;
using ProductManagement.Entities.Products;
using ProductManagement.Permissions;
using ProductManagement.Products;
using ProductManagement.Responses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using ProductManagement.Dtos;

namespace ProductManagement.AppServices.Products;
//[Authorize]
public class ProductAppService : ApplicationService, IProductAppService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductAppService> _logger;
    private readonly ILocalEventBus _localEventBus;
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDistributedCache _cache;
    private readonly string _cacheKeyPrefix;
    private readonly TimeSpan _cacheExpiration;

    #region Constructor
    public ProductAppService(
        IRepository<Product, Guid> productRepository,
        IMapper mapper,
        ILogger<ProductAppService> logger,
        IRepository<Category, Guid> categoryRepository,
        ILocalEventBus localEventBus,
        IWebHostEnvironment hostingEnvironment,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IDistributedCache cache)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _logger = logger;
        _categoryRepository = categoryRepository;
        _localEventBus = localEventBus;
        _hostingEnvironment = hostingEnvironment;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        
        // Initialize cache settings from configuration
        _cacheKeyPrefix = _configuration["Cache:ProductList:KeyPrefix"] ?? "Products:";
        var expirationMinutes = _configuration.GetValue<int>("Cache:ProductList:ExpirationMinutes", 10);
        _cacheExpiration = TimeSpan.FromMinutes(expirationMinutes);
    }
    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new product with the provided information and optional image.
    /// </summary>
    /// <param name="input">The product creation data including optional image file.</param>
    /// <returns>A response containing the created product details.</returns>
    /// <exception cref="UserFriendlyException">Thrown when product creation fails.</exception>
    public async Task<ResponseDataDto<object>> CreateAsync(CreateUpdateProductDto input)
    {
        try
        {
            _logger.LogInformation("Starting product creation process for product: {ProductName}", input.Name);

            var product = _mapper.Map<CreateUpdateProductDto, Product>(input);

            if (input.ImageUrl != null)
            {
                _logger.LogDebug("Processing image upload for product: {ProductName}", input.Name);
                var filePath = await ValidateAndUploadFileAsync(input.ImageUrl);
                product.ImageUrl = filePath;
                _logger.LogDebug("Image uploaded successfully for product: {ProductName}", input.Name);
            }

            await _productRepository.InsertAsync(product);
            _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);

            // Invalidate cache after creating new product
            await InvalidateProductListCacheAsync();

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

            _logger.LogInformation("Product creation completed successfully for ID: {ProductId}", product.Id);
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
            _logger.LogError(ex, "Failed to create product {ProductName}. Error: {ErrorMessage}", 
                input.Name, ex.Message);
            throw new UserFriendlyException("An error occurred while creating the product.", "500");
        }
    }

    //[Authorize(ProductManagementPermissions.Category.Edit)]
    /// <summary>
    /// Updates an existing product with new information and optional image.
    /// </summary>
    /// <param name="id">The ID of the product to update.</param>
    /// <param name="input">The updated product data including optional image file.</param>
    /// <returns>A response containing the updated product details.</returns>
    /// <exception cref="UserFriendlyException">Thrown when product update fails.</exception>
    public async Task<ResponseDataDto<object>> UpdateAsync([Required(ErrorMessage = "Id is required.")] Guid id, [FromForm] CreateUpdateProductDto input)
    {
        try
        {
            _logger.LogInformation("Starting product update process for ID: {ProductId}", id);

            var product = await _productRepository.GetAsync(id);
            _mapper.Map(input, product);

            if (input.ImageUrl != null)
            {
                _logger.LogDebug("Processing image update for product ID: {ProductId}", id);
                var filePath = await ValidateAndUploadFileAsync(input.ImageUrl);
                product.ImageUrl = filePath;
                _logger.LogDebug("Image updated successfully for product ID: {ProductId}", id);
            }

            await _productRepository.UpdateAsync(product);
            _logger.LogInformation("Product updated successfully with ID: {ProductId}", id);

            // Invalidate cache after updating product
            await InvalidateProductListCacheAsync();

            var result = _mapper.Map<Product, ProductDto>(product);

            _logger.LogDebug("Publishing product update events for ID: {ProductId}", id);
            await _localEventBus.PublishAsync(new ProductPriceChangedEvent
            {
                ProductId = result.Id,
                NewPrice = result.Price
            });

            await _localEventBus.PublishAsync(new ProductStockChangedEvent
            {
                ProductId = result.Id,
                NewStockQuantity = result.StockQuantity
            });

            _logger.LogInformation("Product update process completed for ID: {ProductId}", id);
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
            _logger.LogWarning(ex, "User friendly exception occurred while updating product ID {ProductId}: {Message}", 
                id, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update product ID {ProductId}. Error: {ErrorMessage}", 
                id, ex.Message);
            throw new UserFriendlyException("An error occurred while updating the product.", "500");
        }
    }

    /// <summary>
    /// Deletes a product by its ID.
    /// </summary>
    /// <param name="id">The ID of the product to delete.</param>
    /// <returns>A response indicating the success of the deletion operation.</returns>
    /// <exception cref="UserFriendlyException">Thrown when product deletion fails.</exception>
    //[Authorize(ProductManagementPermissions.Category.Delete)]
    public async Task<ResponseDataDto<object>> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Starting product deletion process for ID: {ProductId}", id);

            await _productRepository.DeleteAsync(id);

            // Invalidate cache after deleting product
            await InvalidateProductListCacheAsync();

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
            _logger.LogWarning(ex, "User friendly exception occurred while deleting product ID {ProductId}: {Message}", 
                id, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete product ID {ProductId}. Error: {ErrorMessage}", 
                id, ex.Message);
            throw new UserFriendlyException("An error occurred while deleting the product.", "500");
        }
    }

    /// <summary>
    /// Retrieves a product by its ID.
    /// </summary>
    /// <param name="id">The ID of the product to retrieve.</param>
    /// <returns>A response containing the product details.</returns>
    /// <exception cref="UserFriendlyException">Thrown when product retrieval fails.</exception>
    //[Authorize(ProductManagementPermissions.Category.Default)]
    public async Task<ResponseDataDto<ProductDto>> GetAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Starting product retrieval process for ID: {ProductId}", id);

            var product = await _productRepository.FindAsync(id);

            var products = await _productRepository.GetQueryableAsync();
            var categories = await _categoryRepository.GetQueryableAsync();

            var result = await (from p in products
                                join c in categories on p.CategoryId equals c.Id
                                where p.Id == id
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
            _logger.LogWarning(ex, "User friendly exception occurred while retrieving product ID {ProductId}: {Message}", 
                id, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve product ID {ProductId}. Error: {ErrorMessage}", 
                id, ex.Message);
            throw new UserFriendlyException("An error occurred while retrieving the product.", "500");
        }
    }

    /// <summary>
    /// Retrieves a paginated list of products with optional filtering.
    /// </summary>
    /// <param name="input">Pagination and sorting parameters.</param>
    /// <param name="filter">Filter criteria for the product list.</param>
    /// <returns>A response containing the paginated list of products.</returns>
    /// <exception cref="UserFriendlyException">Thrown when product list retrieval fails.</exception>
    //[Authorize(ProductManagementPermissions.Category.Default)]
    //[Authorize(CustomAuthentication.ApiKeyOrBearerTokenPolicy)]
    public async Task<ResponseDataDto<PagedResultDto<ProductDto>>> GetListAsync(PagedAndSortedResultRequestDto input, ProductFilter filter)
    {
        try
        {
            _logger.LogInformation("Starting product list retrieval process with filter: {@Filter}", filter);

            if (input.Sorting.IsNullOrWhiteSpace())
            {
                input.Sorting = "Name";
            }
            filter.SearchKeyword = filter.SearchKeyword?.Trim()?.ToLower();

            // Generate cache key based on input parameters
            var cacheKey = $"{_cacheKeyPrefix}{input.SkipCount}:{input.MaxResultCount}:{input.Sorting}:{filter.SearchKeyword}";
            
            // Try to get from cache first
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrWhiteSpace(cachedResult))
            {
                _logger.LogInformation("Retrieved product list from cache with key: {CacheKey}", cacheKey);
                return JsonSerializer.Deserialize<ResponseDataDto<PagedResultDto<ProductDto>>>(cachedResult);
            }

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

            _logger.LogInformation("Retrieved {Count} products out of {TotalCount} total", items.Count, totalCount);

            var result = new PagedResultDto<ProductDto>(totalCount, items);
            var response = new ResponseDataDto<PagedResultDto<ProductDto>>
            {
                Success = true,
                Code = 200,
                Message = "Products retrieved successfully.",
                Data = result
            };

            // Cache the result
            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(_cacheExpiration);
            
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                cacheOptions
            );

            // Add the cache key to active keys
            await AddActiveCacheKeyAsync(cacheKey);

            _logger.LogInformation("Cached product list with key: {CacheKey}", cacheKey);

            return response;
        }
        catch (UserFriendlyException ex)
        {
            _logger.LogWarning(ex, "User friendly exception occurred while retrieving product list: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve product list. Error: {ErrorMessage}", ex.Message);
            throw new UserFriendlyException("An error occurred while retrieving the products.", "500");
        }
    }

    /// <summary>
    /// Retrieves a list of categories for dropdown selection.
    /// </summary>
    /// <returns>A response containing the list of categories.</returns>
    /// <exception cref="UserFriendlyException">Thrown when category list retrieval fails.</exception>
    public async Task<ResponseDataDto<DropDownDto[]>> GetCategoriesAsync()
    {
        try
        {
            _logger.LogInformation("Starting category list retrieval process");

            var categories = await _categoryRepository.GetQueryableAsync();

            var result = await AsyncExecuter.ToArrayAsync(
                categories
                .Select(x => new DropDownDto
                {
                    Value = x.Id.ToString(),
                    Name = x.Name
                }).OrderBy(x => x.Name)
            );

            _logger.LogInformation("Retrieved {Count} categories successfully", result.Length);

            return new ResponseDataDto<DropDownDto[]>
            {
                Code = 200,
                Success = true,
                Message = "Data retrieved successfully.",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve category list. Error: {ErrorMessage}", ex.Message);
            throw new UserFriendlyException("An error occurred while retrieving the categories.", "500");
        }
    }

    /// <summary>
    /// Downloads the image associated with a product.
    /// </summary>
    /// <param name="id">The ID of the product whose image to download.</param>
    /// <returns>The image file as an IActionResult.</returns>
    /// <exception cref="UserFriendlyException">Thrown when image download fails.</exception>
    public async Task<IActionResult> DownloadImageAsync([Required(ErrorMessage = "Id is required.")] Guid id)
    {
        try
        {
            _logger.LogInformation("Starting image download process for product ID: {ProductId}", id);

            var product = await _productRepository.GetAsync(id);
            if (string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                _logger.LogWarning("Product ID {ProductId} does not have an image URL", id);
                throw new UserFriendlyException("Product does not have an image URL.");
            }

            var imageUrl = product.ImageUrl;
            var baseUrl = _configuration["App:BaseUrl"];
            if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
            {
                imageUrl = new Uri(new Uri(baseUrl), imageUrl).ToString();
                _logger.LogDebug("Constructed absolute image URL: {ImageUrl}", imageUrl);
            }

            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(imageUrl);

            if (response.IsSuccessStatusCode)
            {
                var fileName = Path.GetFileName(product.ImageUrl);
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                
                _logger.LogInformation("Image downloaded successfully for product '{ProductName}'", product.Name);
                
                return new FileContentResult(imageBytes, contentType)
                {
                    FileDownloadName = fileName
                };
            }
            else
            {
                _logger.LogError("Failed to download image for product ID {ProductId}. Status: {StatusCode}", 
                    id, response.StatusCode);
                throw new UserFriendlyException($"Failed to download image. Status: {response.StatusCode}");
            }
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download image for product ID {ProductId}. Error: {ErrorMessage}", 
                id, ex.Message);
            throw new UserFriendlyException("An error occurred while downloading the product image.", "500");
        }
    }

    /// <summary>
    /// Clears all product-related cache entries.
    /// </summary>
    /// <returns>A response indicating the success of the cache clearing operation.</returns>
    public async Task<ResponseDataDto<object>> ClearProductCacheAsync()
    {
        try
        {
            _logger.LogInformation("Starting product cache clearing process");

            // Get all keys matching the product list prefix
            var cacheKeys = await GetProductListCacheKeysAsync();
            var clearedKeys = new List<string>();
            
            // Remove each cache entry
            foreach (var key in cacheKeys)
            {
                await _cache.RemoveAsync(key);
                clearedKeys.Add(key);
                _logger.LogInformation("Cleared cache key: {CacheKey}", key);
            }

            // Also clear the active keys list
            var activeKeysKey = $"{_cacheKeyPrefix}ActiveKeys";
            await _cache.RemoveAsync(activeKeysKey);
            _logger.LogInformation("Cleared active keys list");

            return new ResponseDataDto<object>
            {
                Success = true,
                Code = 200,
                Message = $"Successfully cleared {clearedKeys.Count} cache entries.",
                Data = new
                {
                    ClearedKeys = clearedKeys,
                    ClearedCount = clearedKeys.Count
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear product cache. Error: {ErrorMessage}", ex.Message);
            throw new UserFriendlyException("An error occurred while clearing the product cache.", "500");
        }
    }

    /// <summary>
    /// Retrieves detailed information for a specific product.
    /// </summary>
    /// <param name="ProductId">The ID of the product to retrieve details for.</param>
    /// <returns>A response containing the product details.</returns>
    /// <exception cref="UserFriendlyException">Thrown when product detail retrieval fails.</exception>
    public async Task<ResponseDataDto<ProductDetailsDto>> GetProductDetailAsync(Guid ProductId)
    {
        try
        {
            _logger.LogInformation("Starting product detail retrieval process for ID: {ProductId}", ProductId);

            var products = await _productRepository.GetQueryableAsync();
            var categories = await _categoryRepository.GetQueryableAsync();

            var result = await (from p in products
                                join c in categories on p.CategoryId equals c.Id
                                where p.Id == ProductId
                                select new ProductDetailsDto
                                {
                                    ProductName = p.Name,
                                    CategoryName = c.Name
                                }).FirstOrDefaultAsync();

            if (result == null)
            {
                _logger.LogWarning("Product not found with ID: {ProductId}", ProductId);
                throw new UserFriendlyException("Product not found.");
            }

            _logger.LogInformation("Product detail retrieved successfully for ID: {ProductId}", ProductId);

            return new ResponseDataDto<ProductDetailsDto>
            {
                Success = true,
                Code = 200,
                Message = "Product detail retrieved successfully.",
                Data = result
            };
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve product detail for ID {ProductId}. Error: {ErrorMessage}",
                ProductId, ex.Message);
            throw new UserFriendlyException("An error occurred while retrieving the product detail.", "500");
        }
    }

    /// <summary>
    /// Retrieves detailed information for all products in a specific category.
    /// </summary>
    /// <param name="categoryId">The ID of the category to retrieve product details for.</param>
    /// <returns>A response containing the list of product details.</returns>
    /// <exception cref="UserFriendlyException">Thrown when product details retrieval fails.</exception>
    public async Task<ResponseDataDto<List<ProductDetailsDto>>> GetProductDetailsAsync(Guid categoryId)
    {
        try
        {
            _logger.LogInformation("Starting product details retrieval process for category ID: {CategoryId}", categoryId);

            var products = await _productRepository.GetQueryableAsync();
            var categories = await _categoryRepository.GetQueryableAsync();

            var result = await (from p in products
                                join c in categories on p.CategoryId equals c.Id
                                where p.CategoryId == categoryId
                                select new ProductDetailsDto
                                {
                                    ProductName = p.Name,
                                    CategoryName = c.Name
                                }).ToListAsync();

            _logger.LogInformation("Retrieved {Count} product details for category ID: {CategoryId}",
                result.Count, categoryId);

            return new ResponseDataDto<List<ProductDetailsDto>>
            {
                Success = true,
                Code = 200,
                Message = "Product details retrieved successfully.",
                Data = result
            };
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve product details for category ID {CategoryId}. Error: {ErrorMessage}",
                categoryId, ex.Message);
            throw new UserFriendlyException("An error occurred while retrieving the product details.", "500");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Invalidates all product list cache entries
    /// </summary>
    private async Task InvalidateProductListCacheAsync()
    {
        try
        {
            // Get all keys matching the product list prefix
            var cacheKeys = await GetProductListCacheKeysAsync();
            
            // Remove each cache entry
            foreach (var key in cacheKeys)
            {
                await _cache.RemoveAsync(key);
                _logger.LogInformation("Invalidated cache key: {CacheKey}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while invalidating product list cache");
        }
    }

    /// <summary>
    /// Gets all cache keys that match the product list prefix
    /// </summary>
    private async Task<IEnumerable<string>> GetProductListCacheKeysAsync()
    {
        try
        {
            // Since Redis doesn't provide a direct way to get all keys with a pattern,
            // we'll use a workaround by storing a list of active cache keys
            var activeKeysKey = $"{_cacheKeyPrefix}ActiveKeys";
            var activeKeysJson = await _cache.GetStringAsync(activeKeysKey);
            
            if (string.IsNullOrEmpty(activeKeysJson))
            {
                return Enumerable.Empty<string>();
            }

            return JsonSerializer.Deserialize<List<string>>(activeKeysJson) ?? Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting product list cache keys");
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Adds a cache key to the list of active keys
    /// </summary>
    private async Task AddActiveCacheKeyAsync(string cacheKey)
    {
        try
        {
            var activeKeysKey = $"{_cacheKeyPrefix}ActiveKeys";
            var activeKeysJson = await _cache.GetStringAsync(activeKeysKey);
            var activeKeys = new List<string>();

            if (!string.IsNullOrEmpty(activeKeysJson))
            {
                activeKeys = JsonSerializer.Deserialize<List<string>>(activeKeysJson) ?? new List<string>();
            }

            if (!activeKeys.Contains(cacheKey))
            {
                activeKeys.Add(cacheKey);
                await _cache.SetStringAsync(
                    activeKeysKey,
                    JsonSerializer.Serialize(activeKeys),
                    new DistributedCacheEntryOptions().SetAbsoluteExpiration(_cacheExpiration)
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding active cache key: {CacheKey}", cacheKey);
        }
    }

    /// <summary>
    /// Validates and uploads a file, returning the file path.
    /// </summary>
    private async Task<string> ValidateAndUploadFileAsync(IFormFile file)
    {
        await ValidateFileAsync(file);
        return await UploadFileAsync(file);
    }

    /// <summary>
    /// Validates a file's size and type.
    /// </summary>
    private async Task ValidateFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("File validation failed: No file was uploaded or the file is empty");
            throw new UserFriendlyException("No file was uploaded or the file is empty.");
        }

        _logger.LogDebug("Validating file: {FileName}", file.FileName);

        var maxFileSize = _configuration.GetValue<int>("FileUpload:MaxSize");

        if (file.Length > maxFileSize)
        {
            var maxSizeMB = maxFileSize / (1024 * 1024);
            _logger.LogWarning("File validation failed: File size {FileSize}MB exceeds maximum allowed size of {MaxSize}MB", 
                file.Length / (1024 * 1024), maxSizeMB);
            throw new UserFriendlyException($"File size should not exceed {maxSizeMB}MB. Current size: {file.Length / (1024 * 1024)}MB");
        }

        var allowedFileTypes = _configuration.GetSection("FileUpload:AllowedTypes").Get<string[]>();
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedFileTypes.Contains(fileExtension))
        {
            _logger.LogWarning("File validation failed: File type {FileType} is not in allowed types: {AllowedTypes}", 
                fileExtension, string.Join(", ", allowedFileTypes));
            throw new UserFriendlyException($"File type {fileExtension} is not allowed. Allowed types: {string.Join(", ", allowedFileTypes)}");
        }

        _logger.LogDebug("File {FileName} validated successfully", file.FileName);
    }

    /// <summary>
    /// Uploads a file to the server and returns the file path.
    /// </summary>
    private async Task<string> UploadFileAsync(IFormFile file)
    {
        _logger.LogInformation("Starting file upload process for: {FileName}", file.FileName);

        var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            _logger.LogInformation("Creating uploads folder at {UploadsFolder}", uploadsFolder);
            Directory.CreateDirectory(uploadsFolder);
        }

        var safeFileName = Path.GetFileNameWithoutExtension(file.FileName)
            .Replace(" ", "-")
            .Replace("_", "-")
            .ToLowerInvariant();
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid():N}-{safeFileName}{fileExtension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using FileStream stream = new FileStream(filePath, FileMode.Create);
        file.CopyTo(stream);

        _logger.LogInformation("File uploaded successfully: {FilePath}", filePath);

        return "/uploads/" + uniqueFileName;
    }

    #endregion
}


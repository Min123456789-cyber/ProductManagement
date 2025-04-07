using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductManagement.Dtos;
using ProductManagement.Entities.Category;
using ProductManagement.Entities.Products;
using ProductManagement.Products;
using ProductManagement.Responses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net.Http;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace ProductManagement.AppServices.Products;

/// <summary>
/// Application service for managing products in the system.
/// Handles CRUD operations, file uploads, and product-related business logic.
/// </summary>
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

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the ProductAppService with required dependencies.
    /// </summary>
    public ProductAppService(
        IRepository<Product, Guid> productRepository,
        IMapper mapper,
        ILogger<ProductAppService> logger,
        IRepository<Category, Guid> categoryRepository,
        ILocalEventBus localEventBus,
        IWebHostEnvironment hostingEnvironment,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _logger = logger;
        _categoryRepository = categoryRepository;
        _localEventBus = localEventBus;
        _hostingEnvironment = hostingEnvironment;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }
    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new product with the provided information and optional image.
    /// </summary>
    /// <param name="input">The product creation data including optional image file.</param>
    /// <returns>A response containing the created product details.</returns>
    /// <exception cref="UserFriendlyException">Thrown when product creation fails.</exception>
    public async Task<ResponseDataDto<object>> CreateAsync([FromForm] CreateUpdateProductDto input)
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
    public async Task<ResponseDataDto<object>> DeleteAsync([Required(ErrorMessage = "Id is required.")] Guid id)
    {
        try
        {
            _logger.LogInformation("Starting product deletion process for ID: {ProductId}", id);

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
    public async Task<ResponseDataDto<ProductDto>> GetAsync([Required(ErrorMessage = "Id is required.")] Guid id)
    {
        try
        {
            _logger.LogInformation("Starting product retrieval process for ID: {ProductId}", id);

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
            var baseUrl = _configuration["Product:BaseUrl"];
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

    #endregion

    #region Private Methods

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


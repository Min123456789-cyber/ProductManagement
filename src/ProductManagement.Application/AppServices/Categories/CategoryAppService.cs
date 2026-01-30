using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProductManagement.Categories;
using ProductManagement.Entities.Category;
using ProductManagement.Responses;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Microsoft.FeatureManagement;
using System.Collections.Generic;

namespace ProductManagement.Application.AppServices.Categories;
public class CategoryAppService : ApplicationService, ICategoryAppService
{
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly ILogger<CategoryAppService> _logger;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly IFeatureManager _featureManager;

    public CategoryAppService(IRepository<Category, Guid> categoryRepository,
        ILogger<CategoryAppService> logger,
        IMapper mapper,
        IMemoryCache cache,
        IFeatureManager featureManager)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
        _mapper = mapper;
        _cache = cache;
        _featureManager = featureManager;
    }

    [Authorize]
    public async Task<ResponseDataDto<CategoryResponseDto>> CreateAsync(CreateUpdateCategoryDto input)
    {
        try
        {
            _logger.LogInformation("CategoryAppService - CreateAsync : Creating new category with name: {CategoryName}", input.Name);

            var category = _mapper.Map<CreateUpdateCategoryDto, Category>(input);
            await _categoryRepository.InsertAsync(category);

            _logger.LogInformation("CategoryAppService - CreateAsync : Category created successfully with ID: {CategoryId}", category.Id);

            var result = _mapper.Map<Category, CategoryDto>(category);

            return new ResponseDataDto<CategoryResponseDto>
            {
                Success = true,
                Code = 200,
                Message = "Category created successfully.",
                Data = new CategoryResponseDto
                {
                    Id = result.Id
                }
            };
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating category with name {CategoryName}: {Message}", input.Name, ex.Message);
            throw new UserFriendlyException("An error occurred while creating the category.", "500");
        }
    }

    public async Task<PagedResultDto<CategoryDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {

        try
        {
            // Feature flag check
            if (!await _featureManager.IsEnabledAsync("CategoryListEnabled"))
            {
                // Option 1: Return empty result
                return new PagedResultDto<CategoryDto>(0, new List<CategoryDto>());
                // Option 2: Throw error
                //throw new UserFriendlyException("Category list feature is disabled.", "FeatureDisabled");
            }

            if (string.IsNullOrWhiteSpace(input.Sorting))
            {
                input.Sorting = "Name";
            }

            var categories = await _categoryRepository.GetQueryableAsync();

            var query = from c in categories
                        select new CategoryDto
                        {
                            Id = c.Id,
                            Name = c.Name
                        };

            var totalCount = await AsyncExecuter.CountAsync(query);

            var items = await AsyncExecuter.ToListAsync(query.
                OrderBy(input.Sorting)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

            return new PagedResultDto<CategoryDto>(totalCount, items);
        }
        catch (Exception ex)
        {
            throw new UserFriendlyException(ex.Message, "Internal server error");
        }
    }
}
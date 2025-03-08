using AutoMapper;
using Microsoft.Extensions.Logging;
using ProductManagement.Categories;
using ProductManagement.Entities.Category;
using ProductManagement.Responses;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace ProductManagement.Application.AppServices.Categories;
public class CategoryAppService : ApplicationService, ICategoryAppService
{
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly ILogger<CategoryAppService> _logger;
    private readonly IMapper _mapper;

    public CategoryAppService(IRepository<Category, Guid> categoryRepository,
        ILogger<CategoryAppService> logger,
        IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ResponseDataDto<object>> CreateAsync(CreateUpdateCategoryDto input)
    {
        try
        {
            _logger.LogInformation("Creating new category with name: {CategoryName}", input.Name);

            var category = _mapper.Map<CreateUpdateCategoryDto, Category>(input);
            await _categoryRepository.InsertAsync(category);

            _logger.LogInformation("Category created successfully with ID: {CategoryId}", category.Id);

            var result = _mapper.Map<Category, CategoryDto>(category);

            return new ResponseDataDto<object>
            {
                Success = true,
                Code = 200,
                Message = "Category created successfully.",
                Data = result
            };
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating category with name {CategoryName}: {Message}",
                input.Name, ex.Message);
            throw new UserFriendlyException("An error occurred while creating the category.", "500");
        }
    }
}
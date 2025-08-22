using ProductManagement.Categories;
using ProductManagement.Entities.Category;
using ProductManagement.Responses;
using System.Threading.Tasks;
using System;
using Volo.Abp;
using ProductManagement.Departments;
using Volo.Abp.Domain.Repositories;
using ProductManagement.Entities.Departments;
using Microsoft.Extensions.Logging;
using AutoMapper;
using ProductManagement.Constants;

namespace ProductManagement.AppServices.Departments;

public class DepartmentAppService : IDepartmentAppService
{
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly ILogger<DepartmentAppService> _logger;
    private readonly IMapper _mapper;

    public DepartmentAppService(IRepository<Department, Guid> departmentRepository,
        ILogger<DepartmentAppService> logger,
        IMapper mapper)
    {
        _departmentRepository = departmentRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ResponseDataDto<DepartmentResponseDto>> CreateAsync(CreateUpdateDepartment input)
    {
        try
        {
            _logger.LogInformation("CategoryAppService - CreateAsync : Creating new category with name: {CategoryName}", input.Name);

            var category = _mapper.Map<CreateUpdateDepartment, Department>(input);
            await _departmentRepository.InsertAsync(category);

            _logger.LogInformation("CategoryAppService - CreateAsync : Category created successfully with ID: {CategoryId}", category.Id);

            var result = _mapper.Map<Department, DepartmentDto>(category);

            return new ResponseDataDto<DepartmentResponseDto>
            {
                Success = true,
                Code = 200,
                Message = "Category created successfully.",
                Data = new DepartmentResponseDto
                {
                    Id = result.Id
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating category with name {CategoryName}: {Message}", input.Name, ex.Message);
            throw new UserFriendlyException(ErrorConsts.ServerError, "500");
        }
    }
}

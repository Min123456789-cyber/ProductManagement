using ProductManagement.Dtos;
using ProductManagement.Responses;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace ProductManagement.Products;

public interface IProductAppService : IApplicationService
{
    Task<ResponseDataDto<object>> CreateAsync(CreateUpdateProductDto input);
    Task<ResponseDataDto<object>> UpdateAsync(Guid id, CreateUpdateProductDto input);
    Task<ResponseDataDto<object>> DeleteAsync(Guid id);
    Task<ResponseDataDto<ProductDto>> GetAsync(Guid id);
    Task<ResponseDataDto<PagedResultDto<ProductDto>>> GetListAsync(PagedAndSortedResultRequestDto input, ProductFilter filter);
    Task<ResponseDataDto<DropDownDto[]>> GetCategoriesAsync(); 
}

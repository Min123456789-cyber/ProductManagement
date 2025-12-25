using ProductManagement.Responses;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace ProductManagement.Categories;

public interface ICategoryAppService
{
    Task<ResponseDataDto<CategoryResponseDto>> CreateAsync(CreateUpdateCategoryDto input);
    Task<PagedResultDto<CategoryDto>> GetListAsync(PagedAndSortedResultRequestDto input);
}

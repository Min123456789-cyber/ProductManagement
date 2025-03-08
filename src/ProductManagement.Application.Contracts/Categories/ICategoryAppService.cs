using ProductManagement.Responses;
using System.Threading.Tasks;

namespace ProductManagement.Categories;

public interface ICategoryAppService
{
    Task<ResponseDataDto<object>> CreateAsync(CreateUpdateCategoryDto input);
}

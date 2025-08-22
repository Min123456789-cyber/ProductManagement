using ProductManagement.Responses;
using System.Threading.Tasks;

namespace ProductManagement.Departments;

public interface IDepartmentAppService
{
    Task<ResponseDataDto<DepartmentResponseDto>> CreateAsync(CreateUpdateDepartment input);
}

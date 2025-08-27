using Microsoft.AspNetCore.Mvc;
using ProductManagement.Responses;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace ProductManagement.Teachers;

public interface ITeacherAppService
{
    Task<ResponseDataDto<TeacherResponseDto>> CreateAsync(CreateUpdateTeacherDto input);
    Task<ResponseDataDto<TeacherResponseDto>> UpdateAsync(Guid id, CreateUpdateTeacherDto input);
    Task<ResponseDataDto<TeacherResponseDto>> DeleteAsync(Guid id);
    Task<ResponseDataDto<PagedResultDto<TeacherDto>>> GetListAsync(PagedAndSortedResultRequestDto input, TeacherFilter filter);
    Task<ResponseDataDto<TeacherDto>> GetAsync(Guid id);
    Task<IActionResult> ExportAsync(TeacherExportRequestDto request);
}

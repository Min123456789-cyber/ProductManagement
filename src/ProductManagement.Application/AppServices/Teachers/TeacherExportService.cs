using Microsoft.EntityFrameworkCore;
using ProductManagement.Entities.Departments;
using ProductManagement.Entities.Teachers;
using ProductManagement.Export;
using ProductManagement.Teachers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace ProductManagement.AppServices.Teachers;

/// <summary>
/// Teacher-specific export service that inherits from the base export service.
/// </summary>
public class TeacherExportService : BaseExportService<Teacher, TeacherExportDto, TeacherFilter>
{
    private readonly IRepository<Teacher, System.Guid> _teacherRepository;
    private readonly IRepository<Department, System.Guid> _departmentRepository;

    public TeacherExportService(
        IExportService<Teacher, TeacherExportDto, TeacherFilter> exportService,
        IRepository<Teacher, System.Guid> teacherRepository,
        IRepository<Department, System.Guid> departmentRepository,
        Microsoft.Extensions.Logging.ILogger<TeacherExportService> logger)
        : base(exportService, logger)
    {
        _teacherRepository = teacherRepository;
        _departmentRepository = departmentRepository;
    }

    /// <summary>
    /// Gets the teacher data to export.
    /// </summary>
    /// <param name="filter">Optional filter for the data.</param>
    /// <returns>The teacher data to export.</returns>
    protected override async Task<IEnumerable<TeacherExportDto>> GetDataAsync(TeacherFilter? filter)
    {
        var teachers = await _teacherRepository.GetQueryableAsync();
        var departments = await _departmentRepository.GetQueryableAsync();

        var query = from t in teachers
                    join d in departments on t.DepartmentId equals d.Id
                    select new TeacherExportDto
                    {
                        Id = t.Id,
                        FirstName = t.FirstName,
                        MiddleName = t.MiddleName,
                        LastName = t.LastName,
                        FullName = $"{t.FirstName} {t.MiddleName} {t.LastName}".Trim(),
                        Email = t.Email,
                        Phone = t.Phone,
                        DepartmentId = t.DepartmentId,
                        DepartmentName = d.Name,
                        CreationTime = t.CreationTime,
                        LastModificationTime = t.LastModificationTime
                    };

        // Apply filter if provided
        if (filter != null && !string.IsNullOrWhiteSpace(filter.SearchKeyword))
        {
            var searchKeyword = filter.SearchKeyword.Trim().ToLower();
            query = query.Where(x =>
                x.FirstName.ToLower().Contains(searchKeyword) ||
                x.LastName.ToLower().Contains(searchKeyword) ||
                x.Email.ToLower().Contains(searchKeyword) ||
                x.DepartmentName.ToLower().Contains(searchKeyword)
            );
        }

        return await query.ToListAsync();
    }
}

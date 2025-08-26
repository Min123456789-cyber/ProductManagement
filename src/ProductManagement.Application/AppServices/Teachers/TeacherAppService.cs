using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProductManagement.Constants;
using ProductManagement.Entities.Departments;
using ProductManagement.Entities.Teachers;
using ProductManagement.Responses;
using ProductManagement.Teachers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace ProductManagement.AppServices.Teachers;

public class TeacherAppService : ITeacherAppService
{
    private readonly IRepository<Teacher, Guid> _teacherRepository;
    private readonly ILogger<TeacherAppService> _logger;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly IRepository<Department, Guid> _departmentRepository;

    public TeacherAppService(IRepository<Teacher, Guid> teacherRepository,
        ILogger<TeacherAppService> logger,
        ICurrentUser currentUser,
        IMapper mapper,
        IRepository<Department, Guid> departmentRepository)
    {
        _teacherRepository = teacherRepository;
        _logger = logger;
        _currentUser = currentUser;
        _mapper = mapper;
        _departmentRepository = departmentRepository;
    }

    /// <summary>
    /// Creates a new teacher record.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public async Task<ResponseDataDto<TeacherResponseDto>> CreateAsync(CreateUpdateTeacherDto input)
    {
        try
        {
            _logger.LogInformation("Starting teacher creation process for Teacher: {TeacherName}", input.FirstName);

            var teacher = _mapper.Map<CreateUpdateTeacherDto, Teacher>(input);

            await _teacherRepository.InsertAsync(teacher);
            _logger.LogInformation("Teacher created successfully with ID: {TeacherId}", teacher.Id);

            var query = _mapper.Map<Teacher, TeacherDto>(teacher);

            _logger.LogInformation("Teacher creation completed successfully for ID: {TeacherId}", teacher.Id);
            return new ResponseDataDto<TeacherResponseDto>
            {
                Success = true,
                Code = 200,
                Message = "Teacher created successfully.",
                Data = new TeacherResponseDto
                {
                    Id = teacher.Id
                }
            };
        }
        catch (Exception ex) when (!(ex is UserFriendlyException))
        {
            _logger.LogError(ex, "Failed to create teacher {TeacherName}. Error: {ErrorMessage}", input.FirstName, ex.Message);
            throw new UserFriendlyException(ErrorConsts.ServerError, "500");
        }
    }

    /// <summary>
    /// Updates an existing teacher record.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public async Task<ResponseDataDto<TeacherResponseDto>> UpdateAsync([Required(ErrorMessage = "Id is required.")] Guid id, CreateUpdateTeacherDto input)
    {
        try
        {
            _logger.LogInformation("Starting teacher update process for ID: {TeacherId}", id);

            var teacher = await _teacherRepository.FindAsync(id);
            if (teacher == null)
            {
                _logger.LogWarning("Teacher with ID: {TeacherId} not found.", id);
                throw new UserFriendlyException(ErrorConsts.NotFound, "404");
            }
            _mapper.Map(input, teacher);

            await _teacherRepository.UpdateAsync(teacher);
            _logger.LogInformation("Teacher updated successfully with ID: {TeacherId}", id);

            var result = _mapper.Map<Teacher, TeacherDto>(teacher);

            _logger.LogInformation("Teacher update process completed for ID: {TeacherId}", id);
            return new ResponseDataDto<TeacherResponseDto>
            {
                Success = true,
                Code = 200,
                Message = "Teacher updated successfully.",
                Data = new TeacherResponseDto
                {
                    Id = teacher.Id
                }
            };
        }
        catch (Exception ex) when(!(ex is UserFriendlyException))
        {
            _logger.LogError(ex, "Failed to update teacher ID {TeacherId}. Error: {ErrorMessage}", id, ex.Message);
            throw new UserFriendlyException(ErrorConsts.ServerError, "500");
        }
    }

    /// <summary>
    /// Deletes a teacher record.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public async Task<ResponseDataDto<TeacherResponseDto>> DeleteAsync([Required(ErrorMessage = "Id is required.")] Guid id)
    {
        try
        {
            _logger.LogInformation("Starting teacher deletion process for ID: {TeacherId}", id);
            var teacher = await _teacherRepository.FindAsync(id);
            if (teacher == null)
            {
                _logger.LogWarning("Teacher with ID: {TeacherId} not found.", id);
                throw new UserFriendlyException(ErrorConsts.NotFound, "404");
            }
            await _teacherRepository.DeleteAsync(id);

            _logger.LogInformation("Teacher deleted successfully with ID: {TeacherId}", id);

            return new ResponseDataDto<TeacherResponseDto>
            {
                Success = true,
                Code = 200,
                Message = "Teacher deleted successfully.",
                Data = new TeacherResponseDto
                {
                    Id = id
                }
            };
        }
        catch (Exception ex) when(!(ex is UserFriendlyException))
        {
            _logger.LogError(ex, "Failed to delete product ID {TeacherId}. Error: {ErrorMessage}", id, ex.Message);
            throw new UserFriendlyException(ErrorConsts.ServerError, "500");
        }
    }

    /// <summary>
    /// Retrieves a paginated list of teachers with optional filtering and sorting.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public async Task<ResponseDataDto<PagedResultDto<TeacherDto>>> GetListAsync(PagedAndSortedResultRequestDto input, TeacherFilter filter)
    {
        try
        {
            _logger.LogInformation("Starting product list retrieval process with filter: {@Filter}", filter);

            if (input.Sorting.IsNullOrWhiteSpace())
            {
                input.Sorting = "Name";
            }
            filter.SearchKeyword = filter.SearchKeyword?.Trim()?.ToLower();

            var teachers = await _teacherRepository.GetQueryableAsync();
            var departments = await _departmentRepository.GetQueryableAsync();

            var query = (from t in teachers
                         join d in departments on t.DepartmentId equals d.Id
                         select new TeacherDto
                         {
                             Id = t.Id,
                             FirstName = t.FirstName,
                             MiddleName = t.MiddleName,
                             LastName = t.LastName,
                             Email = t.Email,
                             Phone = t.Phone,
                             DepartmentId = t.DepartmentId,
                             DepartmentName = d.Name
                         })
                        .WhereIf(!string.IsNullOrWhiteSpace(filter.SearchKeyword),
                            x => x.FirstName.ToLower().Contains(filter.SearchKeyword) ||
                                 x.DepartmentName.ToLower().Contains(filter.SearchKeyword) ||
                                  x.Email.ToLower().Contains(filter.SearchKeyword)
                        );

            var dtos = await query
                .OrderBy(input.Sorting)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToListAsync();

            var totalCount = await query.CountAsync();

            _logger.LogInformation("Teacher list retrieved successfully with {TotalCount} records.", totalCount);

            var teacher = new PagedResultDto<TeacherDto>(totalCount, dtos);

            return new ResponseDataDto<PagedResultDto<TeacherDto>>
            {
                Success = true,
                Code = 200,
                Message = "Teacher list retrieved successfully.",
                Data = teacher
            };
        }
        catch (Exception ex) when(!(ex is UserFriendlyException))
        {
            _logger.LogError(ex, "Failed to retrieve teacher list. Error: {ErrorMessage}", ex.Message);
            throw new UserFriendlyException(ErrorConsts.ServerError, "500");
        }
    }

    /// <summary>
    /// Retrieves a specific teacher by ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public async Task<ResponseDataDto<TeacherDto>> GetAsync([Required(ErrorMessage = "Id is required.")] Guid id)
    {
        try
        {
            _logger.LogInformation("Starting teacher retrieval process for ID: {TeacherId}", id);

            var teacher = await _teacherRepository.FindAsync(id);
            if (teacher == null)
            {
                _logger.LogWarning("Teacher with ID: {TeacherId} not found.", id);
                throw new UserFriendlyException(ErrorConsts.NotFound, "404");
            }

            var teachers = await _teacherRepository.GetQueryableAsync();
            var departments = await _departmentRepository.GetQueryableAsync();

            var result = await (from t in teachers
                                join d in departments on t.DepartmentId equals d.Id
                                where t.Id == id
                                select new TeacherDto
                                {
                                    Id = t.Id,
                                    FirstName = t.FirstName,
                                    MiddleName = t.MiddleName,
                                    LastName = t.LastName,
                                    Email = t.Email,
                                    Phone = t.Phone,
                                    DepartmentId = t.DepartmentId,
                                }).FirstOrDefaultAsync();

            _logger.LogInformation("Teacher retrieved successfully with ID: {TeacherId}", id);

            return new ResponseDataDto<TeacherDto>
            {
                Success = true,
                Code = 200,
                Message = "Teacher retrieved successfully.",
                Data = result
            };
        }
        catch (Exception ex) when (!(ex is UserFriendlyException))
        {
            _logger.LogError(ex, "Failed to retrieve teacher ID {TeacherId}. Error: {ErrorMessage}", id, ex.Message);
            throw new UserFriendlyException(ErrorConsts.ServerError, "500");
        }
    }
}

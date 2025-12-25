using AutoMapper;
using ProductManagement.Categories;
using ProductManagement.Departments;
using ProductManagement.Entities.Category;
using ProductManagement.Entities.Departments;
using ProductManagement.Entities.Products;
using ProductManagement.Entities.Teachers;
using ProductManagement.Products;
using ProductManagement.Teachers;

namespace ProductManagement;

public class ProductManagementApplicationAutoMapperProfile : Profile
{
    public ProductManagementApplicationAutoMapperProfile()
    {
        CreateMap<CreateUpdateCategoryDto, Category>();
        CreateMap<Category, CategoryDto>();

        CreateMap<CreateUpdateProductDto, Product>();
        CreateMap<Product, ProductDto>();

        CreateMap<CreateUpdateDepartment, Department>();
        CreateMap<Department, DepartmentDto>();

        CreateMap<CreateUpdateTeacherDto, Teacher>();
        CreateMap<Teacher, TeacherDto>();
    }
}

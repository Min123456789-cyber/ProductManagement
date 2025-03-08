using AutoMapper;
using ProductManagement.Categories;
using ProductManagement.Entities.Category;
using ProductManagement.Entities.Products;
using ProductManagement.Products;

namespace ProductManagement;

public class ProductManagementApplicationAutoMapperProfile : Profile
{
    public ProductManagementApplicationAutoMapperProfile()
    {
        CreateMap<CreateUpdateCategoryDto, Category>();
        CreateMap<Category, CategoryDto>();

        CreateMap<CreateUpdateProductDto, Product>();
        CreateMap<Product, ProductDto>();
    }
}

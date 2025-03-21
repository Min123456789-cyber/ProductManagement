﻿using ProductManagement.Responses;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace ProductManagement.Products;

public interface IProductAppService
{
    Task<ResponseDataDto<object>> CreateAsync(CreateUpdateProductDto input);
    Task<ResponseDataDto<object>> UpdateAsync(Guid id, CreateUpdateProductDto input);
    Task<ResponseDataDto<object>> DeleteAsync(Guid id);
    Task<ResponseDataDto<object>> GetAsync(Guid id);
    Task<ResponseDataDto<PagedResultDto<ProductDto>>> GetListAsync(PagedAndSortedResultRequestDto input, ProductFilter filter);
}

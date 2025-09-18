using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductManagement.GenericBulkImport;
public interface IBulkImportService
{
    Task<List<T>> GetDataAsync<T>(IFormFile file, string tableIdentifier) where T : IBulkImportDto;
}


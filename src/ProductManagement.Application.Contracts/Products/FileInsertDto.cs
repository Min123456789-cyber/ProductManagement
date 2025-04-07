using Microsoft.AspNetCore.Http;

namespace ProductManagement.Products;

public class FileInsertDto
{
    public IFormFile File { get; set; }
}

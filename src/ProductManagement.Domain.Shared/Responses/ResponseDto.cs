namespace ProductManagement.Responses;

public class ResponseDataDto<T>
{
    public bool Success { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
    public T Data { get; set; }
}

public class ResponseDataDto
{
    public bool Success { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
}
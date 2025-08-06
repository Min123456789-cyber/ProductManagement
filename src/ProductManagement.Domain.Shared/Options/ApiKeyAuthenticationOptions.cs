using Microsoft.AspNetCore.Authentication;

namespace ProductManagement.Options;
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string ApiKey { get; set; }
}

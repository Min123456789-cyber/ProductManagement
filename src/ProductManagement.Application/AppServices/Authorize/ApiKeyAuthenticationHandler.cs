using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductManagement.Options;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace ProductManagement.ApiKeyAuthentication
{
    // Ensure this inherits from AuthenticationHandler<ApiKeyAuthenticationOptions>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Get the API key from the request header
            var apiKey = Request.Headers["x-api-key"].FirstOrDefault();

            if (string.IsNullOrEmpty(apiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("API key is missing."));
            }

            // Compare the provided API key with the configured API key
            if (apiKey == Options.ApiKey)  // Make sure the ApiKey in Options is set correctly
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, "ApiUser") // You can add more claims here
                };

                var identity = new ClaimsIdentity(claims, "ApiKey");
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, "ApiKey");

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }
    }
}

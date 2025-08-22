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
            var apiKey = Request.Headers["x-api-key"].FirstOrDefault();

            if (string.IsNullOrEmpty(apiKey))  //user IsNullOrWhiteSpace from MIN.
            {
                return Task.FromResult(AuthenticateResult.Fail("API key is missing."));
            }

            if (apiKey == Options.ApiKey)  
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, "ApiUser") 
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

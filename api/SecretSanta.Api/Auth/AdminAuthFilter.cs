using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SecretSanta.Api.Auth;

/// <summary>
/// Protects admin endpoints with a single shared password (the ADMIN_PASSWORD environment
/// variable), sent by the admin page in the X-Admin-Password header.
/// </summary>
public class AdminAuthFilter(IConfiguration config) : IAuthorizationFilter
{
    public const string HeaderName = "X-Admin-Password";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var expected = config["ADMIN_PASSWORD"];
        if (string.IsNullOrEmpty(expected))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "ADMIN_PASSWORD is not configured on the server.",
            })
            { StatusCode = StatusCodes.Status503ServiceUnavailable };
            return;
        }

        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (!FixedTimeEquals(provided, expected))
            context.Result = new UnauthorizedResult();
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(a)),
            SHA256.HashData(Encoding.UTF8.GetBytes(b)));
}

/// <summary>Requires the admin password on a controller or action.</summary>
public class AdminOnlyAttribute() : TypeFilterAttribute(typeof(AdminAuthFilter));

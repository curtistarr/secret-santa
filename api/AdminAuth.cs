using System.Security.Cryptography;
using System.Text;

namespace SecretSanta.Api;

/// <summary>
/// Protects admin endpoints with a single shared password (the ADMIN_PASSWORD environment
/// variable), sent by the admin page in the X-Admin-Password header.
/// </summary>
public class AdminAuthFilter(IConfiguration config) : IEndpointFilter
{
    public const string HeaderName = "X-Admin-Password";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = config["ADMIN_PASSWORD"];
        if (string.IsNullOrEmpty(expected))
            return Results.Problem("ADMIN_PASSWORD is not configured on the server.", statusCode: 503);

        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (!FixedTimeEquals(provided, expected))
            return Results.Unauthorized();

        return await next(context);
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(a)),
            SHA256.HashData(Encoding.UTF8.GetBytes(b)));
}

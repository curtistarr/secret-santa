using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using SecretSanta.Api.Auth;

namespace SecretSanta.Api.Tests;

public class AdminAuthFilterTests
{
    private static AuthorizationFilterContext Run(string? configuredPassword, string? headerPassword)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ADMIN_PASSWORD"] = configuredPassword })
            .Build();

        var http = new DefaultHttpContext();
        if (headerPassword is not null)
            http.Request.Headers[AdminAuthFilter.HeaderName] = headerPassword;

        var context = new AuthorizationFilterContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()), []);
        new AdminAuthFilter(config).OnAuthorization(context);
        return context;
    }

    [Fact]
    public void CorrectPassword_IsAllowed()
    {
        Assert.Null(Run("hunter2", "hunter2").Result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong")]
    [InlineData("HUNTER2")]
    public void MissingOrWrongPassword_IsUnauthorized(string? header)
    {
        Assert.IsType<UnauthorizedResult>(Run("hunter2", header).Result);
    }

    [Fact]
    public void UnconfiguredPassword_ReturnsServiceUnavailable()
    {
        var result = Assert.IsType<ObjectResult>(Run(null, "anything").Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
    }
}

using Microsoft.AspNetCore.Mvc;
using SecretSanta.Api.Services;

namespace SecretSanta.Api.Controllers;

/// <summary>Translates service results into HTTP responses.</summary>
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<T> FromResult<T>(ServiceResult<T> result, Func<T, ActionResult>? onSuccess = null) =>
        result.Status == ServiceStatus.Ok
            ? onSuccess?.Invoke(result.Value!) ?? Ok(result.Value)
            : Failure(result);

    protected IActionResult FromResult(ServiceResult result) =>
        result.Status == ServiceStatus.Ok ? NoContent() : Failure(result);

    private ActionResult Failure(ServiceResult result) => result.Status switch
    {
        ServiceStatus.NotFound => NotFound(),
        ServiceStatus.Conflict => ProblemResult(StatusCodes.Status409Conflict, result.Error),
        _ => ProblemResult(StatusCodes.Status400BadRequest, result.Error),
    };

    private static ObjectResult ProblemResult(int status, string? detail) =>
        new(new ProblemDetails { Status = status, Detail = detail }) { StatusCode = status };
}

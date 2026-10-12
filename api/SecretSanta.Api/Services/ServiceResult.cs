namespace SecretSanta.Api.Services;

public enum ServiceStatus
{
    Ok,
    NotFound,
    Invalid,
    Conflict,
}

/// <summary>Outcome of a service operation, which controllers translate into an HTTP response.</summary>
public class ServiceResult(ServiceStatus status, string? error = null)
{
    public ServiceStatus Status { get; } = status;
    public string? Error { get; } = error;

    public static ServiceResult Ok() => new(ServiceStatus.Ok);
    public static ServiceResult NotFound() => new(ServiceStatus.NotFound);
    public static ServiceResult Invalid(string error) => new(ServiceStatus.Invalid, error);
    public static ServiceResult Conflict(string error) => new(ServiceStatus.Conflict, error);
}

public class ServiceResult<T>(ServiceStatus status, T? value = default, string? error = null)
    : ServiceResult(status, error)
{
    public T? Value { get; } = value;

    public static ServiceResult<T> Ok(T value) => new(ServiceStatus.Ok, value);
    public static new ServiceResult<T> NotFound() => new(ServiceStatus.NotFound);
    public static new ServiceResult<T> Invalid(string error) => new(ServiceStatus.Invalid, default, error);
    public static new ServiceResult<T> Conflict(string error) => new(ServiceStatus.Conflict, default, error);
}

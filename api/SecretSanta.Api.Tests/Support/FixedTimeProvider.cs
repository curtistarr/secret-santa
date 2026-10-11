namespace SecretSanta.Api.Tests.Support;

/// <summary>A clock that always returns the same moment, so timestamps can be asserted exactly.</summary>
public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 12, 1, 9, 30, 0, TimeSpan.Zero);

    public FixedTimeProvider() : this(DefaultNow) { }

    public override DateTimeOffset GetUtcNow() => now;
}

using Microsoft.Extensions.Configuration;
using Npgsql;
using SecretSanta.Api.Data;

namespace SecretSanta.Api.Tests;

public class ConnectionStringTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void Resolve_PrefersConfiguredConnectionString()
    {
        var config = Config(new()
        {
            ["ConnectionStrings:Default"] = "Host=configured",
            ["DATABASE_URL"] = "postgresql://user:pass@railway:5432/db",
        });

        Assert.Equal("Host=configured", ConnectionString.Resolve(config));
    }

    [Fact]
    public void Resolve_ConvertsDatabaseUrl()
    {
        var config = Config(new() { ["DATABASE_URL"] = "postgresql://santa:p%40ss%3Aword@db.railway.internal:6543/railway" });

        var result = new NpgsqlConnectionStringBuilder(ConnectionString.Resolve(config));

        Assert.Equal("db.railway.internal", result.Host);
        Assert.Equal(6543, result.Port);
        Assert.Equal("railway", result.Database);
        Assert.Equal("santa", result.Username);
        Assert.Equal("p@ss:word", result.Password);
    }

    [Fact]
    public void Resolve_DefaultsToPort5432()
    {
        var config = Config(new() { ["DATABASE_URL"] = "postgresql://user:pass@localhost/db" });

        Assert.Equal(5432, new NpgsqlConnectionStringBuilder(ConnectionString.Resolve(config)).Port);
    }

    [Fact]
    public void Resolve_WithNothingConfigured_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ConnectionString.Resolve(Config(new())));
    }
}

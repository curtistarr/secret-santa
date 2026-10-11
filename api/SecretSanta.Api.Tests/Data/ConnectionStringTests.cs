using Microsoft.Extensions.Configuration;
using Npgsql;
using SecretSanta.Api.Data;

namespace SecretSanta.Api.Tests.Data;

public class ConnectionStringTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Test]
    public void Resolve_PrefersConfiguredConnectionString()
    {
        var config = Config(new()
        {
            ["ConnectionStrings:Default"] = "Host=configured",
            ["DATABASE_URL"] = "postgresql://user:pass@railway:5432/db",
        });

        Assert.That(ConnectionString.Resolve(config), Is.EqualTo("Host=configured"));
    }

    [Test]
    public void Resolve_ConvertsDatabaseUrl()
    {
        var config = Config(new() { ["DATABASE_URL"] = "postgresql://santa:p%40ss%3Aword@db.railway.internal:6543/railway" });

        var result = new NpgsqlConnectionStringBuilder(ConnectionString.Resolve(config));

        Assert.Multiple(() =>
        {
            Assert.That(result.Host, Is.EqualTo("db.railway.internal"));
            Assert.That(result.Port, Is.EqualTo(6543));
            Assert.That(result.Database, Is.EqualTo("railway"));
            Assert.That(result.Username, Is.EqualTo("santa"));
            Assert.That(result.Password, Is.EqualTo("p@ss:word"));
        });
    }

    [Test]
    public void Resolve_DefaultsToPort5432()
    {
        var config = Config(new() { ["DATABASE_URL"] = "postgresql://user:pass@localhost/db" });

        Assert.That(new NpgsqlConnectionStringBuilder(ConnectionString.Resolve(config)).Port, Is.EqualTo(5432));
    }

    [Test]
    public void Resolve_WithNothingConfigured_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ConnectionString.Resolve(Config(new())));
    }
}

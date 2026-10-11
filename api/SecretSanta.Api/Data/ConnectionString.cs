using Npgsql;

namespace SecretSanta.Api.Data;

public static class ConnectionString
{
    /// <summary>
    /// Resolves the Postgres connection string. Prefers ConnectionStrings:Default, otherwise
    /// converts a URL-style DATABASE_URL (as provided by Railway) into an Npgsql connection string.
    /// </summary>
    public static string Resolve(IConfiguration config)
    {
        var configured = config.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var url = config["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException(
                "No database configured. Set DATABASE_URL or ConnectionStrings__Default.");

        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
        };
        return builder.ConnectionString;
    }
}

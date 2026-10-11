using Microsoft.EntityFrameworkCore;
using SecretSanta.Api.Data;

namespace SecretSanta.Api.Tests;

public static class TestDb
{
    /// <summary>Creates a context backed by a fresh, isolated in-memory database.</summary>
    public static AppDbContext Create() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}

using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Tests.Support;

internal static class TestDb
{
    public static AppDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    public static async Task<AppDbContext> CreateSeededInMemoryAsync()
    {
        var db = CreateInMemory();
        await CatalogSeeder.EnsureAsync(db);
        return db;
    }
}

using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Tests.ServiceTests;

public sealed class DbSeederTests
{
    [Fact]
    public async Task SeedAsync_OnFreshNestedDirectory_CreatesDatabaseMigratesAndSeedsRequiredRows()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "FlashMemo.Tests",
            Guid.NewGuid().ToString("N"),
            "Data");
        string databasePath = Path.Combine(directory, AppDbContext.DbFileName);

        Directory.Exists(directory).Should().BeFalse();

        try
        {
            await using (var db = CreateContext(databasePath))
            {
                var seeder = new DbSeeder(db, Helpers.GetMapper(), directory);

                await seeder.SeedAsync();
                await seeder.SeedAsync();
            }

            await using var verificationDb = CreateContext(databasePath);

            File.Exists(databasePath).Should().BeTrue();
            (await verificationDb.Database.GetAppliedMigrationsAsync())
                .Should().NotBeEmpty();
                
            (await verificationDb.DeckOptions.SingleAsync()).Id.Should().Be(-1);
            (await verificationDb.AppSessionData.SingleAsync()).Id.Should().Be(-1);
        }
        finally
        {
            string testRoot = Path.Combine(Path.GetTempPath(), "FlashMemo.Tests");

            if (directory.StartsWith(testRoot, StringComparison.OrdinalIgnoreCase)
                && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static AppDbContext CreateContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        return new AppDbContext(options);
    }
}

using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlashMemo.Tests.ServiceTests;

public sealed class DbSeederTests : IDisposable
{
    private readonly string testDirectory = Path.Combine(
        Path.GetTempPath(), "FlashMemo.Tests", Guid.NewGuid().ToString("N"));
    private string DataDirectory => Path.Combine(testDirectory, "Data");
    private string DatabasePath => Path.Combine(DataDirectory, AppDbContext.DbFileName);

    [Fact]
    public async Task TrySeedAsync_OnFreshNestedDirectory_CreatesDatabaseMigratesAndSeedsRequiredRows()
    {
        Directory.Exists(DataDirectory).Should().BeFalse();
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);

        await using (var db = CreateContext(DatabasePath))
        {
            var seeder = new DbSeeder(
                db, Helpers.GetMapper(), Helpers.GetSafetyNet(dialogs.Object), DataDirectory);

            (await seeder.TrySeedAsync()).Should().BeTrue();
            (await seeder.TrySeedAsync()).Should().BeTrue();
        }

        await using var verificationDb = CreateContext(DatabasePath);

        File.Exists(DatabasePath).Should().BeTrue();
        (await verificationDb.Database.GetAppliedMigrationsAsync()).Should().NotBeEmpty();
        (await verificationDb.DeckOptions.SingleAsync()).Id.Should().Be(-1);
        (await verificationDb.AppSessionData.SingleAsync()).Id.Should().Be(-1);
        dialogs.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("directory")]
    [InlineData("migration")]
    [InlineData("preset seed")]
    [InlineData("session seed")]
    public async Task TrySeedAsync_OnFailure_LogsExplainsAndReturnsFalseWithoutRetrying(string failingStep)
    {
        Directory.CreateDirectory(testDirectory);

        if (failingStep == "directory")
            await File.WriteAllTextAsync(DataDirectory, "A file blocks the data directory.");
        else
            Directory.CreateDirectory(DataDirectory);

        if (failingStep == "migration")
            await File.WriteAllTextAsync(DatabasePath, "This is not a SQLite database.");

        await using var db = CreateContext(DatabasePath);

        if (failingStep is "preset seed" or "session seed")
        {
            await db.Database.MigrateAsync();
            string triggerSql = failingStep == "preset seed"
                ? "CREATE TRIGGER FailSeed BEFORE INSERT ON DeckOptions BEGIN SELECT RAISE(ABORT, 'Seed failed'); END;"
                : "CREATE TRIGGER FailSeed BEFORE INSERT ON AppSessionData BEGIN SELECT RAISE(ABORT, 'Seed failed'); END;";
            await db.Database.ExecuteSqlRawAsync(triggerSql);
        }

        var logger = new Mock<ILogger<SafetyNet>>();
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(service => service.ShowError(
                It.Is<string>(message => message.Contains("database") && message.Contains("will close")),
                "Error", DialogButtons.OK))
            .Returns(DialogResult.OK);
        var seeder = new DbSeeder(
            db, Helpers.GetMapper(), new SafetyNet(logger.Object, dialogs.Object), DataDirectory);

        (await seeder.TrySeedAsync()).Should().BeFalse();

        logger.Verify(log => log.Log(
            LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.Is<Exception>(error => error != null),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        dialogs.Verify(service => service.ShowError(
            It.IsAny<string>(), "Error", DialogButtons.OK), Times.Once);
        dialogs.VerifyNoOtherCalls();

        if (failingStep is "preset seed" or "session seed")
        {
            await using var verificationDb = CreateContext(DatabasePath);
            (await verificationDb.AppSessionData.AnyAsync()).Should().BeFalse();
            (await verificationDb.DeckOptions.CountAsync())
                .Should().Be(failingStep == "session seed" ? 1 : 0);
        }
    }

    private static AppDbContext CreateContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        return new AppDbContext(options);
    }

    public void Dispose()
    {
        string testRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FlashMemo.Tests"))
            + Path.DirectorySeparatorChar;
        string target = Path.GetFullPath(testDirectory);

        if (target.StartsWith(testRoot, StringComparison.OrdinalIgnoreCase)
            && Directory.Exists(target))
        {
            Directory.Delete(target, recursive: true);
        }
    }
}
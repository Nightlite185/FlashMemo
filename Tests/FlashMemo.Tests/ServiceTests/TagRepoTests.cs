using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Tests.Fakes.Model;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace FlashMemo.Tests.ServiceTests;

public sealed class TagRepoTests : IDisposable
{
    private readonly FakeDbFactory factory = new();

    [Fact]
    public async Task GetOrCreate_ReusesPersistedCaseInsensitiveNameAndKeepsUsersSeparate()
    {
        await SeedUsers();
        var repo = new TagRepo(factory);
        var first = await repo.GetOrCreate(Tag.CreateNew("Study", 7));
        var reused = await repo.GetOrCreate(Tag.CreateNew("study", 7));
        var otherUser = await repo.GetOrCreate(Tag.CreateNew("Study", 8));

        first.Id.Should().BePositive();
        reused.Id.Should().Be(first.Id);
        reused.Name.Should().Be("Study");
        otherUser.Id.Should().NotBe(first.Id);
        await using var db = factory.CreateDbContext();
        (await db.Tags.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task GetOrCreate_WhenAnotherInsertWinsTheRace_ReturnsThatTag()
    {
        await SeedUsers();
        await using var connectionOwner = factory.CreateDbContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionOwner.Database.GetDbConnection())
            .AddInterceptors(new ConcurrentTagInsert())
            .Options;
        var racingFactory = new Mock<IDbContextFactory<AppDbContext>>();
        racingFactory.Setup(value => value.CreateDbContext()).Returns(() => new AppDbContext(options));

        var tag = await new TagRepo(racingFactory.Object).GetOrCreate(Tag.CreateNew("Study", 7));

        tag.Id.Should().BePositive();
        await using var db = factory.CreateDbContext();
        (await db.Tags.SingleAsync()).Id.Should().Be(tag.Id);
    }

    [Fact]
    public async Task GetOrCreate_ForeignKeyFailureStillPropagates()
    {
        var repo = new TagRepo(factory);

        Func<Task> save = () => repo.GetOrCreate(Tag.CreateNew("Study", 999));
        var failure = await save.Should().ThrowAsync<DbUpdateException>();

        failure.Which.InnerException.Should().BeOfType<SqliteException>()
            .Which.SqliteExtendedErrorCode.Should().Be(787);
        await using var db = factory.CreateDbContext();
        (await db.Tags.AnyAsync()).Should().BeFalse();
    }

    private async Task SeedUsers()
    {
        await using var db = factory.CreateDbContext();
        db.Users.AddRange(
            new UserEntity { Id = 7, Name = "First", Options = UserOptions.CreateDefault() },
            new UserEntity { Id = 8, Name = "Second", Options = UserOptions.CreateDefault() });
        await db.SaveChangesAsync();
    }

    // Insert after the lookup and before SaveChanges starts its own transaction.
    private sealed class ConcurrentTagInsert : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var db = eventData.Context!;
            var tag = db.ChangeTracker.Entries<Tag>().Single().Entity;
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO Tags (Name, UserId) VALUES ({tag.Name}, {tag.UserId})", cancellationToken);
            return result;
        }
    }

    public void Dispose() => factory.Dispose();
}
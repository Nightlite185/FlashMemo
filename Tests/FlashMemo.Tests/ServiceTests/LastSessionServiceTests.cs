using System.Collections.Immutable;
using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.ServiceTests;

public sealed class LastSessionServiceTests : IDisposable
{
    private readonly FakeDbFactory factory = new();

    [Fact]
    public async Task UserCaches_AreIsolatedAndRememberTheirOwnLastDeckAndFilters()
    {
        await SeedUsersAndDecksAsync();
        var service = new LastSessionService(factory, NullLogger<LastSessionService>.Instance);

        await service.LoadAppSessionAsync();
        await service.LoadUserCacheAsync(userId: 1);

        service.LastDeckId = 11;
        service.LastFilters = Filters.GetEmpty(1) with
        {
            TagIds = [101L]
        };
        await service.SaveStateAsync();
        await service.SetLastUserAsync(1);

        await service.LoadUserCacheAsync(userId: 2);

        service.LastDeckId.Should().BeNull();
        service.LastFilters!.UserId.Should().Be(2);
        service.LastFilters.TagIds.Should().BeEmpty();

        service.LastDeckId = 21;
        service.LastFilters = Filters.GetEmpty(2) with
        {
            TagIds = [202L]
        };
        await service.SaveStateAsync();
        await service.SetLastUserAsync(2);

        await service.LoadUserCacheAsync(userId: 1);

        service.LastDeckId.Should().Be(11);
        service.LastFilters!.UserId.Should().Be(1);
        service.LastFilters.TagIds.Should().Equal(101L);
        service.LastUserId.Should().Be(2);
    }

    [Fact]
    public async Task LoadingMissingCache_ActivatesDefaultsWithoutWritingUntilSave()
    {
        await SeedUsersAndDecksAsync();
        var service = new LastSessionService(factory, NullLogger<LastSessionService>.Instance);
        await service.LoadAppSessionAsync();
        await service.LoadUserCacheAsync(1);
        await service.SetLastUserAsync(1);
        service.LastDeckId = 11;
        service.LastFilters = Filters.GetEmpty(1) with { TagIds = [101L] };
        await service.SaveStateAsync();

        await service.LoadUserCacheAsync(2);

        service.LastDeckId.Should().BeNull();
        service.LastFilters!.UserId.Should().Be(2);
        service.LastFilters.TagIds.Should().BeEmpty();
        service.LastUserId.Should().Be(1);
        using (var db = factory.CreateDbContext())
        {
            (await db.UserSessionCaches.AnyAsync(cache => cache.UserId == 2)).Should().BeFalse();
            var previous = await db.UserSessionCaches.SingleAsync(cache => cache.UserId == 1);
            previous.LastUsedDeckId.Should().Be(11);
            previous.LastFilters!.TagIds.Should().Equal(101L);
        }

        service.LastDeckId = 21;
        await service.SaveStateAsync();
        using var verification = factory.CreateDbContext();
        var saved = await verification.UserSessionCaches.SingleAsync(cache => cache.UserId == 2);
        saved.LastUsedDeckId.Should().Be(21);
        saved.LastFilters!.UserId.Should().Be(2);
    }

    [Fact]
    public async Task LoadingExistingCache_ChangesMemoryWithoutWritingToTheDatabase()
    {
        await SeedUsersAndDecksAsync();
        var service = new LastSessionService(factory, NullLogger<LastSessionService>.Instance);
        await service.LoadAppSessionAsync();
        await service.LoadUserCacheAsync(1);
        service.LastDeckId = 11;
        await service.SaveStateAsync();

        await service.LoadUserCacheAsync(1);
        service.LastDeckId.Should().Be(11);
        service.LastDeckId = null;
        service.LastFilters = service.LastFilters! with { TagIds = [999L] };

        using var verification = factory.CreateDbContext();
        var saved = await verification.UserSessionCaches.SingleAsync(cache => cache.UserId == 1);
        saved.LastUsedDeckId.Should().Be(11);
        saved.LastFilters!.TagIds.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RememberingUser_WhenDatabaseWriteFails_PreservesRememberedUser(bool clearing)
    {
        await SeedUsersAndDecksAsync();
        bool fail = false;
        var dbFactory = new Mock<IDbContextFactory<AppDbContext>>();
        dbFactory.Setup(db => db.CreateDbContext()).Returns(() =>
            fail ? throw new IOException() : factory.CreateDbContext());
        var service = new LastSessionService(dbFactory.Object, NullLogger<LastSessionService>.Instance);
        await service.LoadAppSessionAsync();
        await service.SetLastUserAsync(1);
        fail = true;

        Func<Task> saving = clearing ? service.ClearLastUserAsync : () => service.SetLastUserAsync(2);
        await saving.Should().ThrowAsync<IOException>();

        service.LastUserId.Should().Be(1);
        using var verification = factory.CreateDbContext();
        (await verification.AppSessionData.SingleAsync()).LastLoadedUserId.Should().Be(1);
    }

    [Fact]
    public async Task FailedCacheLoad_LogsAndActivatesRequestedUsersDefaults()
    {
        await SeedUsersAndDecksAsync();
        bool fail = false;
        var failure = new IOException("Cache load failed");
        var dbFactory = new Mock<IDbContextFactory<AppDbContext>>();
        dbFactory.Setup(db => db.CreateDbContext()).Returns(() =>
            fail ? throw failure : factory.CreateDbContext());
        var logger = new Mock<ILogger<LastSessionService>>();
        var service = new LastSessionService(dbFactory.Object, logger.Object);
        await service.LoadAppSessionAsync();
        await service.LoadUserCacheAsync(1);
        service.LastDeckId = 11;
        service.LastFilters = Filters.GetEmpty(1) with { TagIds = [101L] };
        fail = true;

        await service.LoadUserCacheAsync(2);

        service.LastDeckId.Should().BeNull();
        service.LastFilters!.UserId.Should().Be(2);
        service.LastFilters.TagIds.Should().BeEmpty();
        logger.Verify(value => value.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(), failure, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        logger.VerifyNoOtherCalls();

        // The fallback is a usable cache that can be persisted when the database recovers.
        fail = false;
        service.LastDeckId = 21;
        await service.SaveStateAsync();
        await service.LoadUserCacheAsync(2);
        service.LastDeckId.Should().Be(21);
        service.LastFilters.UserId.Should().Be(2);
    }

    [Fact]
    public async Task UserCache_CannotReferenceAnotherUsersDeck()
    {
        await SeedUsersAndDecksAsync();
        using var db = factory.CreateDbContext();

        db.UserSessionCaches.Add(new UserSessionCache
        {
            UserId = 1,
            LastUsedDeckId = 21
        });

        Func<Task> save = async () => await db.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task RemovingDeck_ClearsItsSessionCacheReference()
    {
        await SeedUsersAndDecksAsync();

        using (var db = factory.CreateDbContext())
        {
            db.UserSessionCaches.Add(new UserSessionCache
            {
                UserId = 1,
                LastUsedDeckId = 11
            });
            await db.SaveChangesAsync();
        }

        var removedIds = await new DeckRepo(factory)
            .RemoveDeck(11);

        using var verificationDb = factory.CreateDbContext();
        var cache = await verificationDb.UserSessionCaches.SingleAsync(c => c.UserId == 1);

        removedIds.Should().Equal(11L);
        cache.LastUsedDeckId.Should().BeNull();
    }

    private async Task SeedUsersAndDecksAsync()
    {
        await new TestsDbSeeder(factory)
            .SeedDefault();

        using var db = factory.CreateDbContext();

        db.Users.AddRange(
            new UserEntity { Id = 1, Name = "First", Options = UserOptions.CreateDefault() },
            new UserEntity { Id = 2, Name = "Second", Options = UserOptions.CreateDefault() });

        db.Decks.AddRange(
            new Deck { Id = 11, Name = "First deck", UserId = 1, OptionsId = -1 },
            new Deck { Id = 21, Name = "Second deck", UserId = 2, OptionsId = -1 });

        await db.SaveChangesAsync();
    }

    public void Dispose() => factory.Dispose();
}
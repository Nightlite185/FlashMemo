using System.Collections.Immutable;
using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Tests.ServiceTests;

public sealed class LastSessionServiceTests : IDisposable
{
    private readonly FakeDbFactory factory = new();

    [Fact]
    public async Task UserCaches_AreIsolatedAndRememberTheirOwnLastDeckAndFilters()
    {
        await SeedUsersAndDecksAsync();
        var service = new LastSessionService(factory);

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
        await new DbSeeder(factory.CreateDbContext(), Helpers.GetMapper())
            .SeedAsync();

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

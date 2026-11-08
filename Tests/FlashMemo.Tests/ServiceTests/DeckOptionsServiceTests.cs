using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Tests.ServiceTests;

public class DeckOptionsServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Remove_ReassignsAllDecksOrRollsBackWhenDeletionFails(bool fails)
    {
        using var factory = new FakeDbFactory();
        var seeder = new TestsDbSeeder(factory);
        await seeder.SeedDefault();
        seeder.SeedUser();
        var service = new DeckOptionsService(factory, Helpers.GetMapper());
        var preset = await service.CreateNew(DeckOptions.CreateNew("Preset", 7));
        await using (var db = factory.CreateDbContext())
        {
            db.Decks.AddRange(
                new Deck { Id = 1, Name = "First", UserId = 7, OptionsId = preset.Id },
                new Deck { Id = 2, Name = "Second", UserId = 7, OptionsId = preset.Id },
                new Deck { Id = 3, Name = "Default", UserId = 7, OptionsId = DeckOptions.DefaultId });
            await db.SaveChangesAsync();
            if (fails)
                await db.Database.ExecuteSqlRawAsync("""
                    CREATE TRIGGER FailPresetDeletion BEFORE DELETE ON DeckOptions
                    BEGIN SELECT RAISE(ABORT, 'Simulated delete failure'); END;
                    """);
        }

        if (fails)
        {
            Func<Task> remove = () => service.Remove(preset.Id);
            await remove.Should().ThrowAsync<SqliteException>();
        }
        else await service.Remove(preset.Id);

        await using var verify = factory.CreateDbContext();
        (await verify.DeckOptions.AnyAsync(options => options.Id == preset.Id)).Should().Be(fails);
        var assignments = await verify.Decks.OrderBy(deck => deck.Id).Select(deck => deck.OptionsId).ToArrayAsync();
        assignments.Should().Equal(fails ? [preset.Id, preset.Id, -1L] : [-1L, -1L, -1L]);
    }
}
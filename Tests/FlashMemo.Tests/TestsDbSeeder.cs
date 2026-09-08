using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Tests;

public class TestsDbSeeder(IDbContextFactory<AppDbContext> factory)
{
    public async Task SeedDefault() // default deck options and app session
    {
        await using var db = factory.CreateDbContext();

        if (!await db.DeckOptions.AnyAsync(d => d.Id == DeckOptions.DefaultId))
        {
            db.DeckOptions.Add(Helpers.GetMapper()
                .Map<DeckOptionsEntity>(DeckOptions.Default));
        }

        if (!await db.AppSessionData.AnyAsync())
            db.AppSessionData.Add(new AppSessionData { Id = -1 });

        await db.SaveChangesAsync();
    }

    public UserEntity SeedUser()
    {
        var db = factory.CreateDbContext();

        var user = new UserEntity()
        {
            Id = 7,
            Name = "lol",
            Options = UserOptions.CreateDefault()
        };

        db.Users.Add(user);

        db.SaveChanges();
        return user;
    }

    public Deck SeedDeck()
    {
        var db = factory.CreateDbContext();

        var deck = new Deck()
        {
            Id = 7,
            UserId = 7,
            Name = "lol",
            OptionsId = -1
        };

        db.Decks.Add(deck);
        db.SaveChanges();

        return deck;
    }
}

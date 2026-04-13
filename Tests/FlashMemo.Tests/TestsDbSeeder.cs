using AutoMapper;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Tests;

public class TestsDbSeeder(IDbContextFactory<AppDbContext> factory)
{
    private readonly IMapper mapper = Helpers.GetMapper();
    public async Task SeedDefault() // default deck options and last session
    {
        await new DbSeeder(
            factory.CreateDbContext(), mapper)
            .SeedAsync();
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
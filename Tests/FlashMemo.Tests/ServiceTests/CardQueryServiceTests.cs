using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FluentAssertions;

namespace FlashMemo.Tests.ServiceTests;

public class CardQueryServiceTests
{
    [Fact]
    public async Task GetAllFromUser_IncludesBrowseDataAndOnlyTheRequestedUsersCards()
    {
        using var factory = new FakeDbFactory();
        await new TestsDbSeeder(factory).SeedDefault();
        var deck = new Deck { Id = 1, UserId = 7, Name = "Deck", OptionsId = -1 };
        var otherDeck = new Deck { Id = 2, UserId = 8, Name = "Other deck", OptionsId = -1 };
        var tag = new Tag { Id = 1, UserId = 7, Name = "Tag" };
        var card = CardEntity.CreateNew(StandardNote.Create("Front", "Back"), deck, [tag]);

        await using (var db = factory.CreateDbContext())
        {
            db.Users.AddRange(
                new UserEntity { Id = 7, Name = "User", Options = UserOptions.CreateDefault() },
                new UserEntity { Id = 8, Name = "Other user", Options = UserOptions.CreateDefault() });
            db.Decks.AddRange(deck, otherDeck);
            db.Cards.AddRange(card,
                CardEntity.CreateNew(StandardNote.Create("Other", "Card"), otherDeck, []));
            await db.SaveChangesAsync();
        }

        var cards = await new CardQueryService(factory, null!, null!).GetAllFromUser(7);

        var loaded = cards.Should().ContainSingle().Subject;
        loaded.Id.Should().Be(card.Id);
        loaded.Deck.Name.Should().Be("Deck");
        loaded.Note.Should().BeOfType<StandardNote>().Which.FrontContent.Should().Be("Front");
        loaded.Tags.Should().ContainSingle().Which.Name.Should().Be("Tag");
    }
}
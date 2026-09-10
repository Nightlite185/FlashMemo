using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FluentAssertions;

namespace FlashMemo.Tests.ServiceTests;

public class DatabaseGeneratedIdTests : IDisposable
{
    private readonly FakeDbFactory factory = new();

    [Fact]
    public async Task CreateApis_ReturnSavedObjectsWithDatabaseGeneratedIds()
    {
        await new TestsDbSeeder(factory).SeedDefault();

        var user = await new UserRepo(factory)
            .CreateNew(UserEntity.Create("user"));
        user.Id.Should().BePositive();

        var deck = await new DeckRepo(factory)
            .AddNewDeck(Deck.CreateNew("deck", user.Id, null));
        deck.Id.Should().BePositive();

        var tag = await new TagRepo(factory)
            .CreateNew(Tag.CreateNew("tag", user.Id));
        tag.Id.Should().BePositive();

        var card = await new CardRepo(factory).AddCard(CardEntity.CreateNew(
            StandardNote.Create("front", "back"), deck, [tag]));
        card.Id.Should().BePositive();
        card.Note.Id.Should().BePositive();

        var options = await new DeckOptionsService(factory, Helpers.GetMapper())
            .CreateNew(DeckOptions.CreateNew("options", user.Id));
        options.Id.Should().BePositive();

        await using var db = factory.CreateDbContext();
        var log = new CardLog
        {
            CardId = card.Id,
            UserId = user.Id,
            Action = CardAction.Modify,
            NewCardState = card.State,
            TimeStamp = DateTime.Now
        };

        db.CardLogs.Add(log);
        await db.SaveChangesAsync();

        log.Id.Should().BePositive();
    }

    public void Dispose() => factory.Dispose();
}

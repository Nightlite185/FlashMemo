using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FluentAssertions;

namespace FlashMemo.Tests.ServiceTests;

public class DeckOptVMBuilderTests : IDisposable
{
    private const long FirstUserId = 101;
    private const long SecondUserId = 202;
    private const long FirstUserPresetId = 1_001;
    private readonly FakeDbFactory factory = new();

    [Theory]
    [InlineData(FirstUserId, 2)]
    [InlineData(SecondUserId, 3)]
    public async Task BuildAllCounted_WhenUsersShareDefaultPreset_CountsOnlyRequestedUsersDecks(
        long userId,
        int expectedDeckCount)
    {
        await SeedUsersPresetsAndDecks();

        var presets = await CreateBuilder()
            .BuildAllCounted(userId);

        presets.Single(p => p.Id == DeckOptions.DefaultId)
            .DeckCount.Should().Be(expectedDeckCount);
    }

    [Fact]
    public async Task BuildAllCounted_WhenUserHasCustomPreset_CountsOnlyThatUsersAssignments()
    {
        await SeedUsersPresetsAndDecks();

        var presets = await CreateBuilder()
            .BuildAllCounted(FirstUserId);

        presets.Should().HaveCount(2);
        presets.Single(p => p.Id == FirstUserPresetId)
            .DeckCount.Should().Be(2);
    }

    private DeckOptVMBuilder CreateBuilder()
    {
        var mapper = Helpers.GetMapper();
        var optionsService = new DeckOptionsService(factory, mapper);

        return new DeckOptVMBuilder(factory, mapper, optionsService);
    }

    private async Task SeedUsersPresetsAndDecks()
    {
        await new TestsDbSeeder(factory).SeedDefault();

        await using var db = factory.CreateDbContext();

        db.Users.AddRange(
            CreateUser(FirstUserId, "first"),
            CreateUser(SecondUserId, "second"));

        db.DeckOptions.Add(Helpers.GetMapper().Map<DeckOptionsEntity>(
            DeckOptions.CreateNew("First user's preset", FirstUserId) with
            {
                Id = FirstUserPresetId
            }));

        db.Decks.AddRange(
            CreateDeck(1, FirstUserId, DeckOptions.DefaultId),
            CreateDeck(2, FirstUserId, DeckOptions.DefaultId),
            CreateDeck(3, FirstUserId, FirstUserPresetId),
            CreateDeck(4, FirstUserId, FirstUserPresetId),
            CreateDeck(5, SecondUserId, DeckOptions.DefaultId),
            CreateDeck(6, SecondUserId, DeckOptions.DefaultId),
            CreateDeck(7, SecondUserId, DeckOptions.DefaultId));

        await db.SaveChangesAsync();
    }

    private static UserEntity CreateUser(long id, string name)
        => new()
        {
            Id = id,
            Name = name,
            Created = DateTime.Now,
            Options = UserOptions.CreateDefault()
        };

    private static Deck CreateDeck(long id, long userId, long presetId)
        => new()
        {
            Id = id,
            UserId = userId,
            Name = $"deck {id}",
            OptionsId = presetId,
            Created = DateTime.Now
        };

    public void Dispose()
    {
        factory.Dispose();
    }
}

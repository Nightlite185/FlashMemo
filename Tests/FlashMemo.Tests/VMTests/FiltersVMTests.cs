using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FlashMemo.Tests.VMTests;

public sealed class FiltersVMTests : IDisposable
{
    private const long UserId = 101;

    private const long RootDeckId = 10;
    private const long NestedDeckId = 11;
    private const long DeeplyNestedDeckId = 12;
    private const long OtherRootDeckId = 20;

    private const long RootCardId = 1_001;
    private const long NestedCardId = 1_002;
    private const long DeeplyNestedCardId = 1_003;
    private const long OtherRootCardId = 1_004;

    private readonly FakeDbFactory factory = new();

    [Fact]
    public async Task RootDeckSelection_WithChildren_ReturnsEntireBranchOnly()
    {
        var result = await FilterCards(
            selectedDeckId: RootDeckId,
            includeChildren: true);

        result.CardIds.Should().Equal(
            RootCardId,
            NestedCardId,
            DeeplyNestedCardId);

        result.FilteredDeckIds.Should().BeEquivalentTo([
            RootDeckId,
            NestedDeckId,
            DeeplyNestedDeckId
        ]);
    }

    [Fact]
    public async Task RootDeckSelection_WithoutChildren_ReturnsDirectCardsOnly()
    {
        var result = await FilterCards(
            selectedDeckId: RootDeckId,
            includeChildren: false);

        result.CardIds.Should().Equal(RootCardId);
        result.FilteredDeckIds.Should().Equal(RootDeckId);
    }

    [Fact]
    public async Task NestedDeckSelection_WithChildren_ReturnsNestedBranchOnly()
    {
        var result = await FilterCards(
            selectedDeckId: NestedDeckId,
            includeChildren: true);

        result.CardIds.Should().Equal(
            NestedCardId,
            DeeplyNestedCardId);

        result.FilteredDeckIds.Should().BeEquivalentTo([
            NestedDeckId,
            DeeplyNestedDeckId
        ]);
    }

    [Fact]
    public async Task DeeplyNestedDeckSelection_DoesNotFallBackToAllCards()
    {
        var result = await FilterCards(
            selectedDeckId: DeeplyNestedDeckId,
            includeChildren: true);

        result.CardIds.Should().Equal(DeeplyNestedCardId);
        result.FilteredDeckIds.Should().Equal(DeeplyNestedDeckId);
        result.CardIds.Should().NotContain(OtherRootCardId);
    }

    private async Task<FilterResult> FilterCards(
        long selectedDeckId,
        bool includeChildren)
    {
        await SeedHierarchy();
        var vm = await CreateViewModel();

        FindDeck(vm.DeckTree, selectedDeckId)
            .IsSelected = true;
        vm.IncludeChildrenDecks = includeChildren;

        var filters = vm.TakeSnapshot();

        filters.DeckIds.Should().NotBeEmpty(
            "a selected deck must never be interpreted as an unfiltered query");

        await using var db = factory.CreateDbContext();
        var cardIds = await db.Cards
            .Where(filters.ToExpression(offset: 0))
            .OrderBy(card => card.Id)
            .Select(card => card.Id)
            .ToArrayAsync();

        return new FilterResult(
            cardIds,
            filters.DeckIds.Distinct().ToArray());
    }

    private async Task<FiltersVM> CreateViewModel()
    {
        var deckRepo = new DeckRepo(factory);
        var deckTreeBuilder = new DeckTreeBuilder(
            deckRepo,
            Mock.Of<ICountingService>());

        var lastSession = new Mock<ILastSessionService>();
        lastSession.SetupProperty(
            service => service.LastFilters,
            Filters.GetEmpty(UserId));

        var vm = new FiltersVM(
            deckTreeBuilder,
            new TagRepo(factory),
            new VMEventBus(),
            lastSession.Object,
            UserId);

        await vm.InitializeAsync();
        return vm;
    }

    private async Task SeedHierarchy()
    {
        await new TestsDbSeeder(factory).SeedDefault();

        await using var db = factory.CreateDbContext();

        db.Users.Add(new UserEntity
        {
            Id = UserId,
            Name = "Filter test user",
            Created = DateTime.Now,
            Options = UserOptions.CreateDefault()
        });

        db.Decks.AddRange(
            CreateDeck(RootDeckId, "Root"),
            CreateDeck(NestedDeckId, "Nested", RootDeckId),
            CreateDeck(DeeplyNestedDeckId, "Deeply nested", NestedDeckId),
            CreateDeck(OtherRootDeckId, "Other root"));

        db.Cards.AddRange(
            CreateCard(RootCardId, RootDeckId),
            CreateCard(NestedCardId, NestedDeckId),
            CreateCard(DeeplyNestedCardId, DeeplyNestedDeckId),
            CreateCard(OtherRootCardId, OtherRootDeckId));

        await db.SaveChangesAsync();
    }

    private static Deck CreateDeck(
        long id,
        string name,
        long? parentDeckId = null)
        => new()
        {
            Id = id,
            Name = name,
            UserId = UserId,
            OptionsId = DeckOptions.DefaultId,
            ParentDeckId = parentDeckId,
            Created = DateTime.Now
        };

    private static CardEntity CreateCard(long id, long deckId)
        => new()
        {
            Id = id,
            UserId = UserId,
            DeckId = deckId,
            Note = new StandardNote
            {
                FrontContent = $"front {id}",
                BackContent = $"back {id}"
            },
            Created = DateTime.Now,
            Interval = TimeSpan.Zero,
            State = CardState.New
        };

    private static DeckNode FindDeck(
        IEnumerable<DeckNode> nodes,
        long deckId)
    {
        foreach (var node in nodes)
        {
            if (node.Id == deckId)
                return node;

            var nested = FindDeckOrDefault(node.Children, deckId);
            if (nested is not null)
                return nested;
        }

        throw new InvalidOperationException($"Deck {deckId} was not found.");
    }

    private static DeckNode? FindDeckOrDefault(
        IEnumerable<DeckNode> nodes,
        long deckId)
    {
        foreach (var node in nodes)
        {
            if (node.Id == deckId)
                return node;

            var nested = FindDeckOrDefault(node.Children, deckId);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    private sealed record FilterResult(
        IReadOnlyList<long> CardIds,
        IReadOnlyList<long> FilteredDeckIds);

    public void Dispose()
    {
        factory.Dispose();
    }
}
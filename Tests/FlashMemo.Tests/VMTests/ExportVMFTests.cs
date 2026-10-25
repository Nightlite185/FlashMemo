using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class ExportVMFTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateAsync_FlattensSelectedTreesWithoutDuplicateDeckIds(bool selectDescendantToo)
    {
        var root = CreateDeck(1);
            var child = CreateDeck(2);
                var grandchild = CreateDeck(3);
        var otherRoot = CreateDeck(4);
        
        root.AddChild(child);
        child.AddChild(grandchild);
        DeckNode[] selection = selectDescendantToo ? [root, child, otherRoot] : [root, otherRoot];
        long[] countedIds = [];
        var counter = new Mock<ICountingService>(MockBehavior.Strict);
        counter.Setup(service => service.AllCardsInDecks(It.IsAny<IEnumerable<long>>()))
            .Callback<IEnumerable<long>>(ids => countedIds = ids.ToArray())
            .ReturnsAsync(1);
        var factory = new ExportVMF(
            Mock.Of<ICardQueryService>(), Mock.Of<INoteSerializer>(), counter.Object);

        var vm = await factory.CreateAsync(7, selection, () => { });

        vm.Should().NotBeNull();
        // CSV preparation requires one ID per deck path. Repeated descendants
        // otherwise trigger a modal error instead of exporting the selection.
        countedIds.Should().OnlyHaveUniqueItems();
        countedIds.Should().BeEquivalentTo(new long[] { 1, 2, 3, 4 });
        counter.Verify(service => service.AllCardsInDecks(It.IsAny<IEnumerable<long>>()), Times.Once);
        counter.VerifyNoOtherCalls();
    }

    private static DeckNode CreateDeck(long id)
        => new(new Deck { Id = id, UserId = 7, Name = $"Deck {id}" }, []);
}

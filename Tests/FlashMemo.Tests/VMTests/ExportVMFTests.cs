using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class ExportVMFTests
{
    [Fact]
    public async Task CreateAsync_NoCardsShowsInformationWithoutCreatingPopup()
    {
        var counter = new Mock<ICountingService>(MockBehavior.Strict);
        counter.Setup(service => service.AllCardsInDecks(It.IsAny<IEnumerable<long>>())).ReturnsAsync(0);
        var query = new Mock<ICardQueryService>(MockBehavior.Strict);
        var exporter = new Mock<IExportService>(MockBehavior.Strict);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(service => service.ShowNoCardsFoundToExport());
        var factory = new ExportVMF(query.Object, exporter.Object, counter.Object, dialogs.Object,
            NullLogger<ExportVM>.Instance);

        var vm = await factory.CreateAsync(7, [CreateDeck(1)], () => { });

        vm.Should().BeNull();
        dialogs.Verify(service => service.ShowNoCardsFoundToExport(), Times.Once);
        dialogs.VerifyNoOtherCalls();
        query.VerifyNoOtherCalls();
        exporter.VerifyNoOtherCalls();
    }

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
            Mock.Of<ICardQueryService>(), Mock.Of<IExportService>(), counter.Object, Mock.Of<IDialogService>(),
            NullLogger<ExportVM>.Instance);

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

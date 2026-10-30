using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlashMemo.Tests.VMTests;

public class DecksVMTests
{
    [Fact]
    public async Task ShowExportMenu_WithNoSelection_ThrowsWithoutCountingCards()
    {
        var counter = new Mock<ICountingService>(MockBehavior.Strict);
        var factory = new ExportVMF(
            Mock.Of<ICardQueryService>(), Mock.Of<IExportService>(), counter.Object, Mock.Of<IDialogService>(),
            NullLogger<ExportVM>.Instance);
        var vm = new DecksVM(null!, null!, null!, null!, null!, 1, null!, factory,
            NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!, Helpers.GetSafetyNet());

        Func<Task> showExport = () => vm.ShowExportMenuCommand.ExecuteAsync(null);

        await showExport.Should().ThrowAsync<InvalidOperationException>();
        vm.CurrentPopup.Should().BeNull();
        counter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShowExportMenu_PassesSelectedTreesToFactory_AndCloseClearsPopup()
    {
        var parent = CreateDeck(1);
        var child = CreateDeck(2);
        var grandchild = CreateDeck(3);
        var unselected = CreateDeck(4);
        parent.AddChild(child);
        child.AddChild(grandchild);
        long[] countedIds = [];
        var counter = new Mock<ICountingService>(MockBehavior.Strict);
        counter.Setup(service => service.AllCardsInDecks(It.IsAny<IEnumerable<long>>()))
            .Callback<IEnumerable<long>>(ids => countedIds = ids.ToArray())
            .ReturnsAsync(1);
        var factory = new ExportVMF(
            Mock.Of<ICardQueryService>(), Mock.Of<IExportService>(), counter.Object, Mock.Of<IDialogService>(),
            NullLogger<ExportVM>.Instance);
        var vm = new DecksVM(null!, null!, null!, null!, null!, 1, null!, factory,
            NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!, Helpers.GetSafetyNet());
        vm.DeckTree.Add(parent);
        vm.DeckTree.Add(unselected);
        vm.SelectedDecks.Add(parent);

        await vm.ShowExportMenuCommand.ExecuteAsync(null);

        countedIds.Should().Equal(1, 2, 3);
        var popup = vm.CurrentPopup.Should().BeOfType<ExportVM>().Subject;
        popup.HasErrors.Should().BeFalse();
        popup.ConfirmCommand.CanExecute(null).Should().BeTrue();
        counter.Verify(service => service.AllCardsInDecks(It.IsAny<IEnumerable<long>>()), Times.Once);
        counter.VerifyNoOtherCalls();

        popup.CloseCommand.Execute(null);

        vm.CurrentPopup.Should().BeNull();
        vm.SelectedDecks.Should().Equal(parent);
    }

    [Fact]
    public async Task ReloadDomain_ClearsSelectionBeforeReplacingTree()
    {
        DeckNode oldDeck = CreateDeck(1);
        DeckNode newDeck = CreateDeck(2);
        var builder = new Mock<IDeckTreeBuilder>();
        builder.Setup(service => service.BuildCountedAsync(1))
            .ReturnsAsync(new[] { newDeck });
        var vm = new DecksVM(null!, builder.Object, null!, null!, null!, 1, null!, null!,
            NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!, Helpers.GetSafetyNet());
        vm.DeckTree.Add(oldDeck);
        vm.SelectedDecks.Add(oldDeck);

        await vm.ReloadDomainCommand.ExecuteAsync(null);

        vm.SelectedDecks.Should().BeEmpty();
        vm.DeckTree.Should().Equal(newDeck);
    }

    [Fact]
    public async Task RemoveDecks_WithNoSelection_DoesNothing()
    {
        var repo = new Mock<IDeckRepo>(MockBehavior.Strict);
        var vm = new DecksVM(repo.Object, null!, null!, null!, null!, 1, null!, null!,
            NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!, Helpers.GetSafetyNet());

        await vm.RemoveDeckCommand.ExecuteAsync(null);

        repo.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemoveDecks_WhenSelectionContainsDescendant_DeletesEachSubtreeOnce(bool descendantFirst)
    {
        var deckRepo = new Mock<IDeckRepo>();
        deckRepo
            .Setup(repo => repo.RemoveDeck(1))
            .ReturnsAsync(new HashSet<long> { 1, 2 });
        deckRepo
            .Setup(repo => repo.RemoveDeck(3))
            .ReturnsAsync(new HashSet<long> { 3 });

        var userOptions = new Mock<IUserOptionsService>();
        userOptions
            .Setup(service => service.GetFromUser(1))
            .ReturnsAsync(new UserOptions { ConfirmDeckDeletion = false });

        var vm = new DecksVM(
            deckRepo.Object,
            null!,
            null!,
            Mock.Of<ILastSessionService>(),
            userOptions.Object,
            1,
            null!,
            null!,
            NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!, Helpers.GetSafetyNet());

        DeckNode parent = CreateDeck(1);
        DeckNode child = CreateDeck(2);
        DeckNode otherRoot = CreateDeck(3);
        parent.AddChild(child);
        vm.DeckTree.Add(parent);
        vm.DeckTree.Add(otherRoot);
        vm.SelectedDecks.Add(descendantFirst ? child : parent);
        vm.SelectedDecks.Add(descendantFirst ? parent : child);
        vm.SelectedDecks.Add(otherRoot);

        await vm.RemoveDeckCommand.ExecuteAsync(null);

        deckRepo.Verify(repo => repo.RemoveDeck(1), Times.Once);
        deckRepo.Verify(repo => repo.RemoveDeck(2), Times.Never);
        deckRepo.Verify(repo => repo.RemoveDeck(3), Times.Once);
        vm.DeckTree.Should().BeEmpty();
        vm.SelectedDecks.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task RemoveDecks_WhenDeletionFails_KeepsFailedDeckAndStopsDeleting(int failedRootId)
    {
        var deckRepo = new Mock<IDeckRepo>(MockBehavior.Strict);
        if (failedRootId == 1)
            deckRepo.Setup(repo => repo.RemoveDeck(1)).ThrowsAsync(new IOException());
        else
            deckRepo.Setup(repo => repo.RemoveDeck(1)).ReturnsAsync(new HashSet<long> { 1 });
        deckRepo.Setup(repo => repo.RemoveDeck(2)).ThrowsAsync(new IOException());

        var userOptions = new Mock<IUserOptionsService>();
        userOptions.Setup(service => service.GetFromUser(1))
            .ReturnsAsync(new UserOptions { ConfirmDeckDeletion = false });
        var lastSession = new Mock<ILastSessionService>(MockBehavior.Strict);
        lastSession.SetupGet(service => service.LastDeckId).Returns(failedRootId);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Returns(DialogResult.OK);
        var vm = new DecksVM(deckRepo.Object, null!, null!, lastSession.Object, userOptions.Object,
            1, null!, null!, NullLogger<DecksVM>.Instance, dialogs.Object, null!, Helpers.GetSafetyNet(dialogs.Object));
        DeckNode[] decks = [CreateDeck(1), CreateDeck(2), CreateDeck(3)];
        foreach (var deck in decks)
        {
            vm.DeckTree.Add(deck);
            vm.SelectedDecks.Add(deck);
        }

        await vm.RemoveDeckCommand.ExecuteAsync(null);

        var remaining = decks[(failedRootId - 1)..];
        vm.DeckTree.Should().Equal(remaining);
        vm.SelectedDecks.Should().Equal(remaining);
        deckRepo.Verify(repo => repo.RemoveDeck(1), Times.Once);
        deckRepo.Verify(repo => repo.RemoveDeck(2), failedRootId == 1 ? Times.Never() : Times.Once());
        deckRepo.VerifyNoOtherCalls();
        lastSession.VerifySet(service => service.LastDeckId = null, Times.Never);
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK), Times.Once);
        dialogs.VerifyNoOtherCalls();
    }

    private static DeckNode CreateDeck(long id)
        => new(
            new Deck
            {
                Id = id,
                UserId = 1,
                Name = $"Deck {id}"
            },
            []);
}

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
            NullLogger<ExportVM>.Instance, Helpers.GetSafetyNet());
        var vm = new DecksVM(null!, null!, null!, null!, null!, 1, null!, factory,
            NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!, Helpers.GetSafetyNet(), Helpers.GetMapper());

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
            NullLogger<ExportVM>.Instance, Helpers.GetSafetyNet());
        var vm = new DecksVM(null!, null!, null!, null!, null!, 1, null!, factory,
            NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!, Helpers.GetSafetyNet(), Helpers.GetMapper());
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
            NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!, Helpers.GetSafetyNet(), Helpers.GetMapper());
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
            NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!, Helpers.GetSafetyNet(), Helpers.GetMapper());

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
            NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!, Helpers.GetSafetyNet(), Helpers.GetMapper());

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
            1, null!, null!, NullLogger<DecksVM>.Instance, dialogs.Object, null!, Helpers.GetSafetyNet(dialogs.Object), Helpers.GetMapper());
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RenameDeck_CommitsSubmittedNameOnlyAfterSuccessfulSave(bool succeeds)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repo = new Mock<IDeckRepo>(MockBehavior.Strict);
        repo.Setup(service => service.RenameDeck(1, "New name")).Returns(completion.Task);
        var dialogs = new Mock<IDialogService>();
        var vm = new DecksVM(repo.Object, null!, null!, null!, null!, 1, null!, null!,
            NullLogger<DecksVM>.Instance, dialogs.Object, null!, Helpers.GetSafetyNet(dialogs.Object), Helpers.GetMapper());
        var deck = CreateDeck(1);
        deck.BeginRenameCommand.Execute(null);
        deck.TempName = "New name";

        var saving = vm.RenameDeckCommand.ExecuteAsync(deck);
        deck.Name.Should().Be("Deck 1");
        deck.SavedEntity.Name.Should().Be("Deck 1");
        deck.IsRenaming.Should().BeTrue();
        deck.TempName.Should().Be("New name");

        if (succeeds)
            completion.SetResult();
        else
            completion.SetException(new IOException());
        await saving;

        deck.Name.Should().Be(succeeds ? "New name" : "Deck 1");
        deck.SavedEntity.Name.Should().Be(succeeds ? "New name" : "Deck 1");
        deck.IsRenaming.Should().Be(!succeeds);
        deck.TempName.Should().Be(succeeds ? null : "New name");
        repo.Verify(service => service.RenameDeck(1, "New name"), Times.Once);
        repo.VerifyNoOtherCalls();
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK),
            succeeds ? Times.Never() : Times.Once());
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, false, false)]
    public async Task ReparentDeck_ChangesTreeAndSavedEntityOnlyAfterSuccessfulSave(
        bool startsAtRoot, bool movesToRoot, bool succeeds)
    {
        var oldParent = startsAtRoot ? null : CreateDeck(2);
        var newParent = movesToRoot ? null : CreateDeck(3);
        var original = new Deck
        {
            Id = 1,
            Name = "Original name",
            UserId = 7,
            OptionsId = 42,
            Created = new DateTime(2025, 6, 15),
            ParentDeckId = oldParent?.Id
        };
        var deck = new DeckNode(original, [],
            countByState: new CardsCount { Lessons = 2, Learning = 3, Reviews = 4 })
        {
            IsSelected = true,
            IsExpanded = true
        };
        var child = CreateDeck(4);
        deck.AddChild(child);
        oldParent?.AddChild(deck);

        // A successful prior rename also updates the saved backing entity.
        deck.BeginRenameCommand.Execute(null);
        deck.TempName = "Renamed deck";
        await deck.RenameAsync(_ => Task.FromResult(true));

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Deck? submitted = null;
        var repo = new Mock<IDeckRepo>(MockBehavior.Strict);
        repo.Setup(service => service.SaveEditedDeck(It.IsAny<Deck>()))
            .Callback<Deck>(edited => submitted = edited)
            .Returns(completion.Task);
        var dialogs = new Mock<IDialogService>();
        var vm = new DecksVM(repo.Object, null!, null!, null!, null!, 7, null!, null!,
            NullLogger<DecksVM>.Instance, dialogs.Object, null!, Helpers.GetSafetyNet(dialogs.Object),
            Helpers.GetMapper());
        vm.DeckTree.Add(oldParent ?? deck);
        if (newParent is not null)
            vm.DeckTree.Add(newParent);
        vm.SelectedDecks.Add(deck);
        var originalRoots = vm.DeckTree.ToArray();

        var saving = vm.ReparentDeckCommand.ExecuteAsync(new(deck, newParent));

        submitted.Should().NotBeNull().And.NotBeSameAs(original);
        submitted!.Id.Should().Be(original.Id);
        submitted.Name.Should().Be("Renamed deck");
        submitted.UserId.Should().Be(original.UserId);
        submitted.OptionsId.Should().Be(original.OptionsId);
        submitted.Created.Should().Be(original.Created);
        submitted.ParentDeckId.Should().Be(newParent?.Id);
        deck.SavedEntity.Should().BeSameAs(original);
        original.ParentDeckId.Should().Be(oldParent?.Id);
        original.Name.Should().Be("Renamed deck");
        deck.Parent.Should().BeSameAs(oldParent);
        vm.DeckTree.Should().Equal(originalRoots);
        oldParent?.Children.Should().Equal(deck);
        newParent?.Children.Should().BeEmpty();
        newParent?.IsExpanded.Should().BeFalse();

        if (succeeds)
            completion.SetResult();
        else
            completion.SetException(new IOException());
        await saving;

        if (succeeds)
        {
            deck.SavedEntity.Should().BeSameAs(submitted);
            deck.Parent.Should().BeSameAs(newParent);
            oldParent?.Children.Should().BeEmpty();
            if (newParent is null)
                vm.DeckTree.Should().Equal(oldParent!, deck);
            else
            {
                newParent.Children.Should().Equal(deck);
                newParent.IsExpanded.Should().BeTrue();
                vm.DeckTree.Should().NotContain(deck);
            }
        }
        else
        {
            deck.SavedEntity.Should().BeSameAs(original);
            deck.Parent.Should().BeSameAs(oldParent);
            vm.DeckTree.Should().Equal(originalRoots);
            oldParent?.Children.Should().Equal(deck);
            newParent?.Children.Should().BeEmpty();
            newParent?.IsExpanded.Should().BeFalse();
        }

        // Moving the existing node preserves its subtree and view state.
        deck.Children.Should().Equal(child);
        child.Parent.Should().BeSameAs(deck);
        deck.IsExpanded.Should().BeTrue();
        deck.IsSelected.Should().BeTrue();
        deck.LessonsCount.Should().Be(2);
        deck.LearningCount.Should().Be(3);
        deck.ReviewsCount.Should().Be(4);
        vm.SelectedDecks.Should().Equal(deck);
        original.ParentDeckId.Should().Be(oldParent?.Id);
        repo.Verify(service => service.SaveEditedDeck(It.IsAny<Deck>()), Times.Once);
        repo.VerifyNoOtherCalls();
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK),
            succeeds ? Times.Never() : Times.Once());
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
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class DecksVMReloadTests
{
    [Theory]
    [InlineData("query")]
    [InlineData(null)]
    public async Task Reload_ReplacesTreeAndClearsSelectionOnlyAfterPreparingTheWholeResult(string? failedStage)
    {
        var oldDeck = Node(1);
        var selectedChild = Node(2);
        oldDeck.AddChild(selectedChild);
        oldDeck.IsExpanded = true;
        selectedChild.IsSelected = true;
        var newDeck = Node(3);
        var pending = new TaskCompletionSource<IList<DeckNode>>();
        var builder = new Mock<IDeckTreeBuilder>(MockBehavior.Strict);
        builder.Setup(service => service.BuildCountedAsync(7)).Returns(pending.Task);
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        if (failedStage is not null)
        {
            dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
                .Returns(DialogResult.Cancel);
        }
        var vm = new DecksVM(null!, builder.Object, null!, null!, null!, 7, new VMEventBus(), null!,
            NullLogger<DecksVM>.Instance, dialogs.Object, null!, Helpers.GetSafetyNet(dialogs.Object), null!);
        vm.DeckTree.Add(oldDeck);
        vm.SelectedDecks.Add(selectedChild);
        int collectionChanges = 0;
        vm.DeckTree.CollectionChanged += (_, _) => collectionChanges++;
        vm.SelectedDecks.CollectionChanged += (_, _) => collectionChanges++;

        var reloading = vm.ReloadDomainCommand.ExecuteAsync(null);
        collectionChanges.Should().Be(0);
        vm.DeckTree.Should().Equal(oldDeck);
        vm.SelectedDecks.Should().Equal(selectedChild);

        if (failedStage == "query")
            pending.SetException(new IOException());
        else
            pending.SetResult(new[] { newDeck });
        await reloading;

        if (failedStage is not null)
        {
            collectionChanges.Should().Be(0);
            vm.DeckTree.Should().Equal(oldDeck);
            vm.SelectedDecks.Should().Equal(selectedChild);
            oldDeck.Children.Should().Equal(selectedChild);
            oldDeck.IsExpanded.Should().BeTrue();
            selectedChild.IsSelected.Should().BeTrue();
        }
        else
        {
            vm.DeckTree.Should().Equal(newDeck);
            vm.SelectedDecks.Should().BeEmpty();
        }

        builder.Verify(service => service.BuildCountedAsync(7), Times.Once);
        dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
            failedStage is null ? Times.Never() : Times.Once());
        dialogs.VerifyNoOtherCalls();
    }

    private static DeckNode Node(long id)
        => new(new Deck { Id = id, UserId = 7, Name = $"Deck {id}" }, []);
}
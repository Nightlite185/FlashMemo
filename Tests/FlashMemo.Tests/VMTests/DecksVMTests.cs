using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class DecksVMTests
{
    [Fact]
    public void SelectDecks_ReplacesSelectionAndSynchronizesDeckState()
    {
        DecksVM vm = CreateViewModel();
        DeckNode first = CreateDeck(1);
        DeckNode second = CreateDeck(2);

        vm.SelectDecks([first]);
        vm.SelectDecks([second]);

        vm.SelectedDecks.Should().Equal(second);
        first.IsSelected.Should().BeFalse();
        second.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void SelectDecks_WhenPreservingExisting_AddsToSelection()
    {
        DecksVM vm = CreateViewModel();
        DeckNode first = CreateDeck(1);
        DeckNode second = CreateDeck(2);

        vm.SelectDecks([first]);
        vm.SelectDecks([second], preserveExisting: true);

        vm.SelectedDecks.Should().Equal(first, second);
        first.IsSelected.Should().BeTrue();
        second.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void ToggleDeckSelection_RemovesAnAlreadySelectedDeck()
    {
        DecksVM vm = CreateViewModel();
        DeckNode first = CreateDeck(1);
        DeckNode second = CreateDeck(2);
        vm.SelectDecks([first, second]);

        vm.ToggleDeckSelection(first);

        vm.SelectedDecks.Should().Equal(second);
        first.IsSelected.Should().BeFalse();
        second.IsSelected.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveDecks_WhenSelectionContainsDescendant_DeletesEachSubtreeOnce()
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
            null!);

        DeckNode parent = CreateDeck(1);
        DeckNode child = CreateDeck(2);
        DeckNode otherRoot = CreateDeck(3);
        parent.AddChild(child);
        vm.DeckTree.Add(parent);
        vm.DeckTree.Add(otherRoot);
        vm.SelectDecks([parent, child, otherRoot]);

        await vm.RemoveDeckCommand.ExecuteAsync(vm.SelectedDecks);

        deckRepo.Verify(repo => repo.RemoveDeck(1), Times.Once);
        deckRepo.Verify(repo => repo.RemoveDeck(2), Times.Never);
        deckRepo.Verify(repo => repo.RemoveDeck(3), Times.Once);
        vm.DeckTree.Should().BeEmpty();
        vm.SelectedDecks.Should().BeEmpty();
        parent.IsSelected.Should().BeFalse();
        child.IsSelected.Should().BeFalse();
        otherRoot.IsSelected.Should().BeFalse();
    }

    private static DecksVM CreateViewModel()
        => new(null!, null!, null!, null!, null!, 1, null!);

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

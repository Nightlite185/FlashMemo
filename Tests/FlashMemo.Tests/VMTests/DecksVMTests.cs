using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;

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

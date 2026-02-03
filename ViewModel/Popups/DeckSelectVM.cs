using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Popups;

public partial class DeckSelectVM
    (Func<Deck, Task> confirm, Action cancel, IEnumerable<DeckNode> deckTree)
    : PopupVMBase(cancel)
{
    public override async Task Confirm()
    {
        await confirm(ChosenDeckNode.Deck);
    }

    public ObservableCollection<DeckNode> DeckTree { get; init; } = [..deckTree];

    [ObservableProperty]
    public partial DeckNode ChosenDeckNode { get; set; }
}
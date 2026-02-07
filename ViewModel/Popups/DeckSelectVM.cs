using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Popups;

public partial class DeckSelectVM
    (Func<DeckNode, Task> confirm, Action cancel, IEnumerable<DeckNode> deckTree)
    : PopupVMBase(cancel)
{
    public override async Task Confirm()
    {
        await confirm(ChosenDeckNode);
    }

    public ObservableCollection<DeckNode> DeckTree { get; init; } = [..deckTree];

    [ObservableProperty]
    public partial DeckNode ChosenDeckNode { get; set; }
}
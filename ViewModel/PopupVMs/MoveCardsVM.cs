using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.BaseVMs;
using FlashMemo.ViewModel.WrapperVMs;

namespace FlashMemo.ViewModel.PopupVMs;

public partial class MoveCardsVM
    (Func<Deck, Task> confirm, Action cancel)
    : PopupVMBase(cancel)
{
    public override async Task Confirm()
    {
        await confirm(ChosenDeckNode.Deck);
    }

    [ObservableProperty]
    public partial DeckNode ChosenDeckNode { get; set; }
}
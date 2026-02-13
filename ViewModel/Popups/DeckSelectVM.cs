using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Popups;

public partial class DeckSelectVM
    (Func<IDeckMeta, Task> confirm, Action cancel, IEnumerable<DeckNode> deckTree)
    : PopupVMBase(cancel)
{
    protected override bool CanConfirm => SelectedDeck is not null;
    protected override async Task Confirm()
    {
        await confirm(SelectedDeck);
        Close();
    }

    public ObservableCollection<DeckNode> DeckTree { get; init; } = [..deckTree];

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    public partial DeckNode SelectedDeck { get; set; }
}
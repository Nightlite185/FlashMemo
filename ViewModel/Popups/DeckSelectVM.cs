using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.Extensions.Logging;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Popups;

public partial class DeckSelectVM(Func<IDeckMeta, Task> confirm, Action cancel, 
                                IEnumerable<DeckNode> deckTree, ILogger<DeckSelectVM> logger, 
                                IDialogService dialogs, SafetyNet net): PopupVMBase(cancel)
{
    protected override bool CanConfirm => SelectedDeck is not null;
    protected override async Task Confirm()
    {
        if (!await net.TryUnsafe(
            () => confirm(SelectedDeck),
            actionTried: "confirm the selected deck",
            retry: false))
            return;

        Close();
    }

    public ObservableCollection<DeckNode> DeckTree { get; init; } = [..deckTree];

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    public partial DeckNode SelectedDeck { get; set; }
}

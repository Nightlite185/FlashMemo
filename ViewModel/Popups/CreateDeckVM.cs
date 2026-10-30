using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Popups;

public partial class CreateDeckVM(Func<string, DeckNode?, Task> confirmAction, Action cancel,
                                    ILogger<CreateDeckVM> logger, IDialogService dialogs, SafetyNet net,
                                    DeckNode? parent = null) : PopupVMBase(cancel)
{
    private readonly Func<string, DeckNode?, Task> confirm = confirmAction;

    protected override bool CanConfirm
        => !string.IsNullOrWhiteSpace(NameField);

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    public partial string NameField { get; set; }

    protected override async Task Confirm()
    {
        if (!await net.TryUnsafe(
            () => confirm(NameField, parent),
            actionTried: "create the deck",
            retry: false))
            return;

        Close();
    }
}
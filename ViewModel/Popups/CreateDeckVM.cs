using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Popups;

public partial class CreateDeckVM(Func<string, DeckNode?, Task> confirmAction, 
                        Action cancel, DeckNode? parent = null): PopupVMBase(cancel)
{
    private readonly Func<string, DeckNode?, Task> confirm = confirmAction;

    protected override bool CanConfirm
        => !string.IsNullOrWhiteSpace(NameField);

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    public partial string NameField { get; set; }

    protected override async Task Confirm()
    {
        await confirm(NameField, parent);
        Close();
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Popups;

public partial class EnterNameVM(Func<string, Task> confirmAction, Action cancel): PopupVMBase(cancel)
{
    private readonly Func<string, Task> confirm = confirmAction;

    protected override bool CanConfirm 
        => !string.IsNullOrWhiteSpace(NameField);

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    public partial string NameField { get; set; }

    protected override async Task Confirm()
    {
        await confirm(NameField);
        Close();
    }
}
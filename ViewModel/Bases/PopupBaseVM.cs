using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlashMemo.ViewModel.Bases;

public abstract partial class PopupVMBase
    (Action cancel): ObservableObject
{
    private readonly Action cancel = cancel;

    [RelayCommand]
    protected void Close() => cancel();

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    protected abstract Task Confirm();

    protected virtual bool CanConfirm => true;
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlashMemo.ViewModel.BaseVMs;

public abstract partial class PopupVMBase
    (Action cancel): ObservableObject
{
    private readonly Action cancel = cancel;

    [RelayCommand]
    public void Cancel() => cancel();

    [RelayCommand]
    public abstract Task Confirm();
}

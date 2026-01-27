using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.ViewModel.BaseVMs;

namespace FlashMemo.ViewModel.PopupVMs;

public partial class PostponeVM
    (Func<int, bool, Task> confirm, Action cancel)
    : PopupVMBase(cancel)
{
    public override async Task Confirm() => await confirm(PostponeByDays, KeepInterval);
    private readonly Func<int, bool, Task> confirm = confirm;

    [ObservableProperty]
    public partial bool KeepInterval { get; set; }

    [ObservableProperty]
    public partial int PostponeByDays { get; set; }
}
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Popups;

public partial class PostponeVM
    (Func<int, bool, Task> confirm, Action cancel)
    : PopupVMBase(cancel)
{
    private readonly Func<int, bool, Task> confirm = confirm;
    public override async Task Confirm()
    {
        await confirm(PostponeByDays, KeepInterval);
        Close();
    }

    [ObservableProperty]
    public partial bool KeepInterval { get; set; }

    [ObservableProperty]
    public partial int PostponeByDays { get; set; }
}
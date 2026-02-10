using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Popups;

public partial class RescheduleVM 
    (Func<DateTime, bool, Task> confirm, Action cancel)
    : PopupVMBase(cancel)
{
    public override async Task Confirm()
    {
        await confirm(RescheduleToDate, KeepInterval);
        Close();
    }
    private readonly Func<DateTime, bool, Task> confirm = confirm;

    [ObservableProperty]
    public partial DateTime RescheduleToDate { get; set; }
    
    [ObservableProperty]
    public partial bool KeepInterval { get; set; }
}

using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Popups;

public partial class EnterNameVM(Func<string, Task> confirmAction, Action cancel): PopupVMBase(cancel)
{
    private readonly Func<string, Task> confirm = confirmAction;
    
    [ObservableProperty] 
    public partial string NameField { get; set; }

    public override async Task Confirm()
    {
        await confirm(NameField);
        Close();
    }
}
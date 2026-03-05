using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Popups;

public partial class NoteTypeSelectVM(Func<NoteTypes, Task> confirm, Action cancel): PopupVMBase(cancel)
{
    public IEnumerable<NoteTypes> NoteTypes 
        => Enum.GetValues<NoteTypes>();

    [ObservableProperty] public partial NoteTypes SelectedType { get; set; }
    
    protected override async Task Confirm()
    {
        await confirm(SelectedType);
        Close();
    }
}
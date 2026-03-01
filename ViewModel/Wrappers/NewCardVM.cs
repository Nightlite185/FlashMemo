using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel.Wrappers;

public partial class NewCardVM: ObservableObject
{
    [ObservableProperty] 
    public partial NoteVM Note { get; set; }

    public ObservableCollection<TagVM> Tags { get; init; } = [];
}
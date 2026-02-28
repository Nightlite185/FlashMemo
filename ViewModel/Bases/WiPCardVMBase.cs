using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Bases;

public abstract partial class WiPCardVMBase: ObservableObject, IViewModel
{
    [ObservableProperty] 
    public partial NoteVM Note { get; set; }

    public ObservableCollection<TagVM> Tags { get; init; } = [];
}
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel.Wrappers;

public partial class NewCardVM(): ObservableObject, IViewModel
{
    [ObservableProperty]
    public partial string FrontContentXAML { get; set; } = "";

    [ObservableProperty]
    public partial string BackContentXAML { get; set; } = "";

    public ObservableCollection<TagVM> Tags { get; init; } = [];
}
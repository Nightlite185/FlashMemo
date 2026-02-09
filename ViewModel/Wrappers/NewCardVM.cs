using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel.Wrappers;

public partial class NewCardVM(): ObservableObject, IViewModel
{
    [ObservableProperty]
    public partial string FrontContent { get; set; } = "";

    [ObservableProperty]
    public partial string BackContent { get; set; } = "";

    public ObservableCollection<TagVM> Tags { get; init; } = [];
}
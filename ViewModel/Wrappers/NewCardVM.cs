using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel.Wrappers;

public partial class NewCardVM(DeckNode deck): ObservableObject, IViewModel
{
    [ObservableProperty]
    public partial DeckNode Deck { get; set; } = deck;

    [ObservableProperty]
    public partial string FrontContent { get; set; } = "";

    [ObservableProperty]
    public partial string BackContent { get; set; } = "";

    public ObservableCollection<TagVM> Tags { get; init; } = [];
}